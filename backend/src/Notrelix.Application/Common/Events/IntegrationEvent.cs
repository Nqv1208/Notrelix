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
        Guid eventId,
        string messageName,
        int schemaVersion,
        Guid correlationId,
        Guid? sourceEventId = null,
        Guid? accountId = null,
        Guid? workspaceId = null,
        Guid? actorUserId = null,
        Guid? causationId = null,
        DateTimeOffset? occurredAt = null,
        bool requireAccountId = false)
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("EventId cannot be empty.", nameof(eventId));
        if (correlationId == Guid.Empty)
            throw new ArgumentException("CorrelationId cannot be empty.", nameof(correlationId));
        if (requireAccountId && (accountId is null || accountId == Guid.Empty))
            throw new ArgumentException("Account-scoped events require a non-empty AccountId.", nameof(accountId));

        EventId = eventId;
        MessageName = messageName;
        SchemaVersion = schemaVersion;
        CorrelationId = correlationId;
        SourceEventId = sourceEventId;
        AccountId = accountId;
        WorkspaceId = workspaceId;
        ActorUserId = actorUserId;
        CausationId = causationId;
        OccurredAt = occurredAt ?? DateTimeOffset.UtcNow;
    }
}
