namespace Notrelix.Application.Events.Governance;

[EventName("governance.permission.revoked", Version = 1)]
public sealed record ResourcePermissionRevokedIntegrationEvent(
    Guid EventId,
    Guid PermissionId,
    Guid? WorkspaceId,
    string ResourceKind,
    Guid ResourceId,
    string SubjectType,
    Guid SubjectId,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "governance.permission.revoked",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: null,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
