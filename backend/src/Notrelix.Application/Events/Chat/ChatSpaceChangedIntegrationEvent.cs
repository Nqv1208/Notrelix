namespace Notrelix.Application.Events.Chat;

/// <summary>
/// A pin of a space changed (UC-CHT-F06, F07): a message was pinned (ChangeKind "pinned") or its pin ended by an unpin or a
/// deletion ("unpinned"), or the channel's permission overrides changed ("permissions", UC-CHT-H01; MessageId is then empty). It
/// reaches the channel stream as a notify-only frame; readers refetch the pins, or the channel and its permissions, over REST.
/// </summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("chat.space.changed", Version = 1)]
public sealed record ChatSpaceChangedIntegrationEvent(
    Guid EventId,
    Guid? AccountId,
    Guid? WorkspaceId,
    Guid SpaceId,
    Guid MessageId,
    string ChangeKind,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "chat.space.changed",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
