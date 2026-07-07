namespace Notrelix.Application.Events.Governance;

[EventName("governance.permission.granted", Version = 1)]
public sealed record ResourcePermissionGrantedIntegrationEvent(
    Guid EventId,
    Guid PermissionId,
    Guid? WorkspaceId,
    string ResourceType,
    Guid ResourceId,
    string SubjectType,
    Guid SubjectId,
    string PermissionLevel,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "governance.permission.granted",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: null,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
