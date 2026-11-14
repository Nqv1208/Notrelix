namespace Notrelix.Application.Events.Chat;

[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("chat.message.deleted", Version = 1)]
public sealed record ChatMessageDeletedIntegrationEvent(
    Guid EventId,
    Guid? AccountId,
    Guid? WorkspaceId,
    Guid MessageId,
    Guid SpaceId,
    long Sequence,
    Guid AuthorId,
    Guid DeletedBy,
    long Version,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default,
    Guid? ThreadRootId = null
) : IntegrationEvent(
    eventId: EventId,
    messageName: "chat.message.deleted",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
