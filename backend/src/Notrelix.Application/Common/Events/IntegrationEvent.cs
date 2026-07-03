namespace Notrelix.Application.Common.Events;

public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; }
    public Guid? SourceEventId { get; }
    public string MessageName { get; }
    public int SchemaVersion { get; }
    public Guid? AccountId { get; }
    public Guid? WorkspaceId { get; }
    public Guid? ActorUserId { get; }
    public Guid CorrelationId { get; }
    public Guid? CausationId { get; }
    public DateTimeOffset OccurredAt { get; }

    protected IntegrationEvent(
        string messageName,
        int schemaVersion,
        Guid? sourceEventId = null,
        Guid? accountId = null,
        Guid? workspaceId = null,
        Guid? actorUserId = null,
        Guid correlationId = default,
        Guid? causationId = null,
        DateTimeOffset? occurredAt = null)
    {
        EventId = Guid.CreateVersion7();
        MessageName = messageName;
        SchemaVersion = schemaVersion;
        SourceEventId = sourceEventId;
        AccountId = accountId;
        WorkspaceId = workspaceId;
        ActorUserId = actorUserId;
        CorrelationId = correlationId;
        CausationId = causationId;
        OccurredAt = occurredAt ?? DateTimeOffset.UtcNow;
    }
}
