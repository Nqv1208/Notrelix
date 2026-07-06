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
    public DateTimeOffset ProcessedAt { get; private set; }
    public string Result { get; private set; } = "Succeeded";
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
        DateTimeOffset processedAt)
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
        ProcessedAt = processedAt;
    }
}
