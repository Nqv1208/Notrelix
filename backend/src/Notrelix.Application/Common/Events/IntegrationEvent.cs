namespace Notrelix.Application.Common.Events;

public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; }
    public Guid? SourceEventId { get; }
    public string MessageName { get; }
    public int SchemaVersion { get; }
    public Guid? WorkspaceId { get; }
    public Guid? ActorUserId { get; }
    public string? CorrelationId { get; }
    public string? CausationId { get; }
    public DateTimeOffset OccurredAt { get; }

    protected IntegrationEvent(
        string messageName,
        int schemaVersion,
        Guid? sourceEventId = null,
        Guid? workspaceId = null,
        Guid? actorUserId = null,
        string? correlationId = null,
        string? causationId = null,
        DateTimeOffset? occurredAt = null)
    {
        EventId = Guid.CreateVersion7();
        MessageName = messageName;
        SchemaVersion = schemaVersion;
        SourceEventId = sourceEventId;
        WorkspaceId = workspaceId;
        ActorUserId = actorUserId;
        CorrelationId = correlationId;
        CausationId = causationId;
        OccurredAt = occurredAt ?? DateTimeOffset.UtcNow;
    }
}
