using Notrelix.Application.Common.Events;

namespace Notrelix.Application.Events.Governance;

[EventName("governance.permission.revoked", Version = 1)]
public sealed record ResourcePermissionRevokedIntegrationEvent(
    Guid PermissionId,
    Guid? WorkspaceId,
    string ResourceType,
    Guid ResourceId,
    string SubjectType,
    Guid SubjectId,
    Guid? ActorUserId = null,
    Guid CorrelationId = default,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "governance.permission.revoked",
    1,
    sourceEventId: null,
    accountId: null,
    WorkspaceId,
    ActorUserId,
    CorrelationId,
    CausationId,
    OccurredAt
);
