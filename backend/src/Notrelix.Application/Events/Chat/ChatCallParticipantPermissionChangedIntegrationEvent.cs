namespace Notrelix.Application.Events.Chat;

/// <summary>
/// A person in a call may publish media or may not any more: the media room applies it to their live connection. The consumer calls the
/// media gateway and retries until it succeeds (at-least-once); applying the same value twice is harmless. It carries the internal room
/// name and no token, and never reaches a client.
/// </summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("chat.call.participant-permission-changed", Version = 1)]
public sealed record ChatCallParticipantPermissionChangedIntegrationEvent(
    Guid EventId,
    Guid? AccountId,
    Guid? WorkspaceId,
    Guid SpaceId,
    Guid CallId,
    Guid PersonId,
    string RoomKey,
    bool CanPublish,
    bool CanPublishVideo,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "chat.call.participant-permission-changed",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
