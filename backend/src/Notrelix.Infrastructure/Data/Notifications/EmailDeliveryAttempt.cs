using System.Text.Json;

namespace Notrelix.Infrastructure.Data.Notifications;

public sealed class EmailDeliveryAttempt
{
    public Guid Id { get; private set; }
    public Guid EmailOutboxId { get; private set; }
    public int AttemptNo { get; private set; }
    public string? Provider { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string Status { get; private set; } = null!;
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int? DurationMs { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public JsonDocument ProviderResponseJson { get; private set; } = JsonDocument.Parse("{}");

    private EmailDeliveryAttempt() { }

    public void MarkSent(string? providerMessageId, DateTimeOffset completedAt)
    {
        Status = "Sent";
        ProviderMessageId = providerMessageId;
        CompletedAt = completedAt;
        DurationMs = (int)(completedAt - StartedAt).TotalMilliseconds;
    }

    public void Restart(DateTimeOffset startedAt)
    {
        Status = "InProgress";
        StartedAt = startedAt;
        CompletedAt = null;
        DurationMs = null;
        ErrorCode = null;
        ErrorMessage = null;
    }

    public void MarkFailed(string? errorCode, string? errorMessage, DateTimeOffset completedAt)
    {
        Status = "Failed";
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        CompletedAt = completedAt;
        DurationMs = (int)(completedAt - StartedAt).TotalMilliseconds;
    }

    public EmailDeliveryAttempt(
        Guid emailOutboxId,
        int attemptNo,
        string? provider,
        string? providerMessageId,
        string status,
        DateTimeOffset startedAt)
    {
        Id = Guid.CreateVersion7();
        EmailOutboxId = emailOutboxId;
        AttemptNo = attemptNo;
        Provider = provider;
        ProviderMessageId = providerMessageId;
        Status = status;
        StartedAt = startedAt;
    }
}
