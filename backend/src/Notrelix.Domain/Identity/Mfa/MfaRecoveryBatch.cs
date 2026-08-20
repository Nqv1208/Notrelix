using Notrelix.Domain.Identity.Mfa.Events;

namespace Notrelix.Domain.Identity.Mfa;

/// <summary>
/// A batch of one-time recovery codes for a User.
///
/// The batch is the consistency boundary for recovery-material lifecycle:
/// consuming a code is a one-time mutation, and regenerating a batch
/// invalidates every unused code of the previous batch atomically.
/// Raw codes are never stored; only per-code verifiers (hashes) are kept.
/// </summary>
public sealed class MfaRecoveryBatch : AggregateRoot
{
    public Guid UserId { get; private set; }
    public DateTimeOffset? InvalidatedAt { get; private set; }

    private readonly List<MfaRecoveryCode> _codes = new();
    public IReadOnlyCollection<MfaRecoveryCode> Codes => _codes;

    private MfaRecoveryBatch() : base() { }

    public static MfaRecoveryBatch Create(
        Guid userId,
        IEnumerable<string> codeHashes,
        DateTimeOffset createdAt,
        Guid actorId)
    {
        Guard.NotEmpty(userId);

        var hashes = codeHashes?
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Distinct(StringComparer.Ordinal)
            .ToArray() ?? [];

        MfaMethodRules.EnsureValidRecoveryBatch(hashes);

        var batch = new MfaRecoveryBatch
        {
            UserId = userId,
            InvalidatedAt = null
        };

        foreach (var hash in hashes)
        {
            batch._codes.Add(MfaRecoveryCode.Create(batch.Id, hash));
        }

        batch.SetAuditOnCreate(actorId, createdAt);
        batch.RaiseDomainEvent(new MfaRecoveryBatchCreatedDomainEvent(batch.Id, userId, hashes.Length, createdAt));

        return batch;
    }

    /// <summary>
    /// Attempts to consume a one-time recovery code identified by its verifier.
    /// Returns false for: an invalidated batch, an unknown verifier, or a code
    /// that was already consumed. Consuming succeeds at most once per code.
    /// </summary>
    public bool TryConsume(string codeHash, DateTimeOffset consumedAt, Guid actorId)
    {
        Guard.NotNullOrWhiteSpace(codeHash);

        if (InvalidatedAt is not null)
        {
            return false;
        }

        var code = _codes.FirstOrDefault(c =>
            c.ConsumedAt is null &&
            string.Equals(c.CodeHash, codeHash, StringComparison.Ordinal));

        if (code is null)
        {
            return false;
        }

        code.MarkConsumed(consumedAt);

        var pending = PrepareAuditUpdate(UserId, consumedAt);
        ApplyAuditUpdate(pending);
        IncrementVersion();

        RaiseDomainEvent(new MfaRecoveryCodeConsumedDomainEvent(Id, UserId, consumedAt));

        return true;
    }

    /// <summary>
    /// Invalidates every unused code of this batch. Used when a new batch is
    /// generated; old codes can never authenticate again.
    /// </summary>
    public void Invalidate(DateTimeOffset invalidatedAt, Guid actorId)
    {
        if (InvalidatedAt is not null)
        {
            return;
        }

        InvalidatedAt = invalidatedAt;

        var pending = PrepareAuditUpdate(UserId, invalidatedAt);
        ApplyAuditUpdate(pending);
        IncrementVersion();

        RaiseDomainEvent(new MfaRecoveryBatchInvalidatedDomainEvent(Id, UserId, invalidatedAt));
    }
}
