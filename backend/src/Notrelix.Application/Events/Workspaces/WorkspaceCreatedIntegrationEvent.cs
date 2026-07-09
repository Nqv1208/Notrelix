namespace Notrelix.Application.Events.Workspaces;

[EventName("workspace.created", Version = 1)]
public sealed record WorkspaceCreatedIntegrationEvent(
    Guid EventId,
    Guid? WorkspaceId,
    string Name,
    string Slug,
    Guid OwnerId,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "workspace.created",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: null,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
