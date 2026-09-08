namespace Notrelix.Infrastructure.Data.Integrations;

/// <summary>
/// M8 AI-FLOW-07 — technical inbound-webhook receipt: Infrastructure
/// reliability state (not a business model). The raw provider callback is
/// captured, verified, deduplicated by (provider, external event id), and
/// then handed to the Application use case. Rejected callbacks are recorded
/// for bounded operational diagnostics only — never business processing
/// state. Forbidden by TAC-GATE-024: this is NOT WebhookDelivery (outbound)
/// and NOT InboundWebhookEvent (frozen Domain legacy gap).
/// </summary>
public class InboundWebhookReceipt
{
    public Guid Id { get; private set; }
    public string Provider { get; private set; } = null!;
    public string ExternalEventId { get; private set; } = null!;
    public string PayloadHash { get; private set; } = null!;
    public DateTimeOffset ReceivedAt { get; private set; }
    public string Status { get; private set; } = null!;
    public DateTimeOffset? ProcessedAt { get; private set; }
    public string? FailureReason { get; private set; }

    private InboundWebhookReceipt() { }

    public static InboundWebhookReceipt Capture(
        string provider,
        string externalEventId,
        string payloadHash,
        DateTimeOffset receivedAt)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider is required.", nameof(provider));
        if (string.IsNullOrWhiteSpace(externalEventId))
            throw new ArgumentException("External event id is required.", nameof(externalEventId));
        if (string.IsNullOrWhiteSpace(payloadHash))
            throw new ArgumentException("Payload hash is required.", nameof(payloadHash));

        return new InboundWebhookReceipt
        {
            Id = Guid.CreateVersion7(),
            Provider = provider,
            ExternalEventId = externalEventId,
            PayloadHash = payloadHash,
            ReceivedAt = receivedAt,
            Status = "Captured"
        };
    }

    public void MarkProcessed(DateTimeOffset processedAt)
    {
        if (Status is not ("Captured" or "Failed")) return;
        Status = "Processed";
        ProcessedAt = processedAt;
    }

    public void MarkFailed(string reason, DateTimeOffset failedAt)
    {
        if (Status is not "Captured") return;
        Status = "Failed";
        FailureReason = reason;
    }

    /// <summary>
    /// A rejected callback (bad signature/timestamp) is recorded as Rejected
    /// for bounded diagnostics — never business processing state.
    /// </summary>
    public static InboundWebhookReceipt CaptureRejected(
        string provider,
        string payloadHash,
        string reason,
        DateTimeOffset receivedAt)
    {
        var receipt = Capture(provider, $"rejected:{Guid.CreateVersion7()}", payloadHash, receivedAt);
        receipt.Status = "Rejected";
        receipt.FailureReason = reason;
        return receipt;
    }
}
