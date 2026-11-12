namespace Notrelix.Application.Events.Chat;

[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("chat.message.created", Version = 1)]
public sealed record ChatMessageCreatedIntegrationEvent(
    Guid EventId,
    Guid? AccountId,
    Guid? WorkspaceId,
    Guid MessageId,
    Guid SpaceId,
    long Sequence,
    Guid AuthorId,
    string Kind,
    string? Content,
    string? ClientMessageId,
    long Version,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default,
    Guid? ReplyToId = null,
    Guid? ThreadRootId = null,
    long? ThreadSequence = null
) : IntegrationEvent(
    eventId: EventId,
    messageName: "chat.message.created",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
