using System.Text.Json;

namespace Notrelix.Infrastructure.Data.Messaging;

public sealed class OutboxDeliveryAttempt
{
    public Guid Id { get; private set; }
    public Guid OutboxMessageId { get; private set; }
    public Guid EventId { get; private set; }
    public int AttemptNo { get; private set; }
    public string? DispatcherId { get; private set; }
    public string? Broker { get; private set; }
    public string? Destination { get; private set; }
    public string Status { get; private set; } = null!;
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public int? DurationMs { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public JsonDocument ErrorDetailJson { get; private set; } = JsonDocument.Parse("{}");

    private OutboxDeliveryAttempt() { }

    public OutboxDeliveryAttempt(
        Guid outboxMessageId,
        Guid eventId,
        int attemptNo,
        string? dispatcherId,
        string? broker,
        string? destination,
        string status,
        DateTimeOffset startedAt)
    {
        Id = Guid.CreateVersion7();
        OutboxMessageId = outboxMessageId;
        EventId = eventId;
        AttemptNo = attemptNo;
        DispatcherId = dispatcherId;
        Broker = broker;
        Destination = destination;
        Status = status;
        StartedAt = startedAt;
    }

    public void MarkSucceeded(DateTimeOffset completedAt)
    {
        Status = "Succeeded";
        CompletedAt = completedAt;
        DurationMs = (int)(completedAt - StartedAt).TotalMilliseconds;
    }

    public void MarkFailed(string errorCode, string errorMessage, DateTimeOffset completedAt)
    {
        Status = "Failed";
        CompletedAt = completedAt;
        DurationMs = (int)(completedAt - StartedAt).TotalMilliseconds;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
    }
}
