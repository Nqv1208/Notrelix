namespace Notrelix.Application.Events.Workspaces;

[EventName("space.created", Version = 1)]
public sealed record SpaceCreatedIntegrationEvent(
    Guid EventId,
    Guid? WorkspaceId,
    Guid SpaceId,
    string Name,
    string Visibility,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "space.created",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: null,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: null,
    occurredAt: OccurredAt
);
