using Notrelix.Application.Common.Events;

namespace Notrelix.Application.Events.Governance;

[EventName("governance.permission.granted", Version = 1)]
public sealed record ResourcePermissionGrantedIntegrationEvent(
    Guid PermissionId,
    Guid? WorkspaceId,
    string ResourceType,
    Guid ResourceId,
    string SubjectType,
    Guid SubjectId,
    string PermissionLevel,
    Guid? ActorUserId = null,
    string? CorrelationId = null,
    string? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "governance.permission.granted",
    1,
    sourceEventId: null,
    WorkspaceId,
    ActorUserId,
    CorrelationId,
    CausationId,
    OccurredAt
);
