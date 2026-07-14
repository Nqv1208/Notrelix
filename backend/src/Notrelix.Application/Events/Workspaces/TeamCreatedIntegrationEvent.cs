namespace Notrelix.Application.Events.Workspaces;

[EventName("team.created", Version = 1)]
public sealed record TeamCreatedIntegrationEvent(
    Guid EventId,
    Guid? WorkspaceId,
    Guid TeamId,
    string Name,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "team.created",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: null,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: null,
    occurredAt: OccurredAt
);
