namespace Notrelix.Infrastructure.Messaging;

/// <summary>
/// Durable claim state for a message whose consumer owns the full
/// provider-effect protocol (acquire → out-of-transaction effect → settle).
/// </summary>
public enum MessageClaimState
{
    /// <summary>No claim row exists — the message may be acquired and processed.</summary>
    Missing,
    /// <summary>A "Processing" claim exists — an attempt is in flight or was interrupted.</summary>
    Processing,
    /// <summary>A "Succeeded" claim exists — the effect is fully settled and must not be re-run.</summary>
    Succeeded,
}

/// <summary>
/// Snapshot of an existing claim. <see cref="State"/> is <see cref="MessageClaimState.Missing"/>
/// when no row exists for the (event, consumer) pair.
/// </summary>
public sealed record MessageClaimInspection(MessageClaimState State, DateTimeOffset? ClaimedAt);

/// <summary>
/// Claim protocol used by consumers that own the complete claim lifecycle for a
/// provider-visible effect (e.g. the n8n dispatch endpoint). The consumer
/// acquires the claim atomically with its first transaction, performs the
/// external effect OUTSIDE any database transaction, then settles the claim
/// atomically with the outcome evidence in a second transaction.
/// </summary>
public interface IProviderEffectClaimStore
{
    /// <summary>
    /// Attempts to insert a "Processing" claim. Returns true when the claim was
    /// acquired (unique on (messageId, consumerName)) and false when a claim
    /// already exists. Must NOT be called inside a transaction that would be
    /// left aborted by the unique violation — handle acquire failure by
    /// inspecting the existing claim in a fresh transaction.
    /// </summary>
    Task<bool> TryAcquireClaimAsync(
        Guid messageId,
        string consumerName,
        string messageName,
        int messageVersion,
        Guid? sourceEventId,
        Guid? workspaceId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads the current claim state for (messageId, consumerName) without
    /// creating anything. Best invoked in a fresh transaction after a failed
    /// acquire, so the residual state can be reconciled safely.
    /// </summary>
    Task<MessageClaimInspection> InspectClaimAsync(
        Guid messageId,
        string consumerName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Releases a "Processing" claim by deleting the row (e.g. so a retryable
    /// outcome can be re-delivered and re-acquired under the same identity).
    /// Returns true when a claim was actually deleted. Only the consumer owning
    /// the current attempt should release — the row delete is scoped to
    /// <c>Status == "Processing"</c> so a settled claim is never removed.
    /// </summary>
    Task<bool> TryReleaseProcessingClaimAsync(
        Guid messageId,
        string consumerName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks an existing claim as Succeeded. No-op when the claim row does not
    /// exist. Implied success marker to persist atomically with the outcome
    /// evidence in the consumer's settle transaction.
    /// </summary>
    void MarkClaimSucceeded(
        Guid messageId,
        string consumerName,
        DateTimeOffset processedAt);
}