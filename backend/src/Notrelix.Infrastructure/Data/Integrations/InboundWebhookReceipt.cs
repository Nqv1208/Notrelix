using System.ComponentModel.DataAnnotations.Schema;

namespace Notrelix.Infrastructure.Data.Integrations;

/// <summary>
/// M8 AI-FLOW-07 — technical inbound-webhook receipt: Infrastructure
/// reliability state (not a business model). The raw provider callback is
/// captured, verified, and claimed per connection by
/// (connection, provider, external event id); the provider-neutral processing
/// message is then consumed by the tenant-scoped processing seam, which
/// decides the terminal state. Rejected callbacks are telemetry-only — never
/// business processing state. Forbidden
/// by TAC-GATE-024: this is NOT WebhookDelivery (outbound) and NOT
/// InboundWebhookEvent (frozen Domain legacy gap).
///
/// ConnectionId is the trusted provenance/binding identity resolved from the
/// WebhookPath → CalendarIntegration → IntegrationConnection bootstrap — never
/// from the payload. It is required for every new accepted receipt (the
/// canonical provider event-id only namespaces a delivery within a provider
/// calendar/connection, so dedup is connection-scoped: UNIQUE(connection_id,
/// provider, external_event_id)). Legacy rows created under the former
/// provider-wide dedup domain are retained only for migration compatibility.
/// Rejected callbacks are not receipt rows. Provenance identity is not the
/// dedup key by itself.
///
/// The payload hash is SHA-256 over the exact verified raw bytes (the same
/// bytes the signature was verified against — never a re-serialization); the
/// raw payload itself is persisted only encrypted at rest. The
/// (connection_id, provider, external_event_id) unique index is the dedup
/// authority.
/// </summary>
public class InboundWebhookReceipt
{
    public Guid Id { get; private set; }
    public Guid? AccountId { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public Guid? ConnectionId { get; private set; }
    public string Provider { get; private set; } = null!;
    public string ExternalEventId { get; private set; } = null!;
    public string PayloadHash { get; private set; } = null!;
    public string? ProtectedPayload { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public string Status { get; private set; } = null!;
    public DateTimeOffset? ProcessedAt { get; private set; }
    public DateTimeOffset? TerminalAt { get; private set; }
    public string? FailureCode { get; private set; }
    public string? FailureDetail { get; private set; }

    [NotMapped]
    public string? FailureReason => FailureDetail;

    public enum ReceiptStatus
    {
        Captured,
        Processed,
        Blocked,
        Failed,
    }

    private InboundWebhookReceipt() { }

    public static InboundWebhookReceipt Capture(
        Guid accountId,
        Guid workspaceId,
        Guid connectionId,
        string provider,
        string externalEventId,
        string payloadHash,
        string? protectedPayload,
        DateTimeOffset receivedAt)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("Account id is required.", nameof(accountId));
        if (workspaceId == Guid.Empty)
            throw new ArgumentException("Workspace id is required.", nameof(workspaceId));
        if (connectionId == Guid.Empty)
            throw new ArgumentException("Connection id is required.", nameof(connectionId));
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider is required.", nameof(provider));
        if (string.IsNullOrWhiteSpace(externalEventId))
            throw new ArgumentException("External event id is required.", nameof(externalEventId));
        if (string.IsNullOrWhiteSpace(payloadHash))
            throw new ArgumentException("Payload hash is required.", nameof(payloadHash));

        return new InboundWebhookReceipt
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId,
            WorkspaceId = workspaceId,
            ConnectionId = connectionId,
            Provider = provider,
            ExternalEventId = externalEventId,
            PayloadHash = payloadHash,
            ProtectedPayload = protectedPayload,
            ReceivedAt = receivedAt,
            Status = "Captured"
        };
    }

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        if (Status is not "Captured") return;
        Status = "Processed";
        ProcessedAt = processedAt;
        TerminalAt = processedAt;
        FailureCode = null;
        FailureDetail = null;
    }

    public void MarkFailed(string reason, DateTimeOffset failedAt, string failureCode = "TechnicalFailure")
    {
        if (Status is not "Captured") return;
        Status = "Failed";
        TerminalAt = failedAt;
        FailureCode = failureCode;
        FailureDetail = reason;
    }

    /// <summary>
    /// TAC v2.6 WAVE-E — durable non-Processed terminal reconciliation state.
    /// The claim was executed by the provider-neutral processing seam under the
    /// derived tenant, but the downstream semantic target could not be applied
    /// because it is not yet defined (WAVE-E BLOCKED-DECISION). "Blocked" is
    /// explicit and terminal — it is never a false "Processed", it is not
    /// retried, and it is not silently dropped back to a retryable state.
    /// </summary>
    public void MarkBlocked(string reason, DateTimeOffset blockedAt)
    {
        if (Status is not "Captured") return;
        Status = "Blocked";
        TerminalAt = blockedAt;
        FailureCode = "SemanticTargetUndefined";
        FailureDetail = reason;
    }
}
