namespace Notrelix.Application.Events.Chat;

/// <summary>
/// A person was released from a call because their access to its space ended: the media room must drop them. The consumer calls the media
/// gateway and retries until it succeeds (at-least-once); removing a person who is already gone is harmless. It carries the internal room
/// name and no token, and never reaches a client.
/// </summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("chat.call.participant-revoked", Version = 1)]
public sealed record ChatCallParticipantRevokedIntegrationEvent(
    Guid EventId,
    Guid? AccountId,
    Guid? WorkspaceId,
    Guid SpaceId,
    Guid CallId,
    Guid PersonId,
    string RoomKey,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "chat.call.participant-revoked",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
