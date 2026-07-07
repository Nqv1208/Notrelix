namespace Notrelix.Application.Events.Workspaces;

[EventName("workspace.member.added", Version = 1)]
public sealed record WorkspaceMemberAddedIntegrationEvent(
    Guid EventId,
    Guid? WorkspaceId,
    Guid UserId,
    string Role,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "workspace.member.added",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: null,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
