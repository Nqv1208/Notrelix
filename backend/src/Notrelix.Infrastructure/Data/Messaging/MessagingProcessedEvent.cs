using System.Text.Json;

namespace Notrelix.Infrastructure.Data.Messaging;

public sealed class MessagingProcessedEvent
{
    public Guid Id { get; private set; }
    public Guid EventId { get; private set; }
    public string ConsumerName { get; private set; } = null!;
    public string? SourceContext { get; private set; }
    public string MessageName { get; private set; } = null!;
    public int MessageVersion { get; private set; } = 1;
    public Guid? SourceEventId { get; private set; }
    public string? SubjectType { get; private set; }
    public Guid? SubjectId { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? CausationId { get; private set; }
    public string Status { get; private set; } = "Processing";
    public DateTimeOffset ClaimedAt { get; private set; }
    public DateTimeOffset? ProcessedAt { get; private set; }
    public DateTimeOffset? FailedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public JsonDocument MetadataJson { get; private set; } = JsonDocument.Parse("{}");

    private MessagingProcessedEvent() { }

    public MessagingProcessedEvent(
        Guid eventId,
        string consumerName,
        string? sourceContext,
        string messageName,
        int messageVersion,
        Guid? sourceEventId,
        string? subjectType,
        Guid? subjectId,
        Guid? workspaceId,
        Guid? actorUserId,
        string? correlationId,
        string? causationId,
        DateTimeOffset claimedAt)
    {
        Id = Guid.CreateVersion7();
        EventId = eventId;
        ConsumerName = consumerName;
        SourceContext = sourceContext;
        MessageName = messageName;
        MessageVersion = messageVersion;
        SourceEventId = sourceEventId;
        SubjectType = subjectType;
        SubjectId = subjectId;
        WorkspaceId = workspaceId;
        ActorUserId = actorUserId;
        CorrelationId = correlationId;
        CausationId = causationId;
        Status = "Processing";
        ClaimedAt = claimedAt;
    }

    public void MarkSucceeded(DateTimeOffset processedAt)
    {
        Status = "Succeeded";
        ProcessedAt = processedAt;
    }

    public void MarkFailed(DateTimeOffset failedAt, string errorMessage)
    {
        Status = "Failed";
        FailedAt = failedAt;
        ErrorMessage = errorMessage;
    }
}
