namespace Notrelix.Application.Events.Chat;

/// <summary>
/// A reaction of a space's message changed (UC-CHT-G06, G07): a person added a reaction of one kind (ChangeKind "added") or removed
/// theirs ("removed"). It carries no content and no counts; it reaches the channel stream as a compact frame, and a reader applies
/// it or refetches the message's reaction summary over REST.
/// </summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("chat.reaction.changed", Version = 1)]
public sealed record ChatReactionChangedIntegrationEvent(
    Guid EventId,
    Guid? AccountId,
    Guid? WorkspaceId,
    Guid SpaceId,
    Guid MessageId,
    string Kind,
    string ChangeKind,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "chat.reaction.changed",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
