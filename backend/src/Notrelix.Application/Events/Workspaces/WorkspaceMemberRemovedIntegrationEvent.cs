using Notrelix.Application.Common.Events;

namespace Notrelix.Application.Events.Workspaces;

[EventName("workspace.member.removed", Version = 1)]
public sealed record WorkspaceMemberRemovedIntegrationEvent(
    Guid? WorkspaceId,
    Guid UserId,
    Guid? ActorUserId = null,
    string? CorrelationId = null,
    string? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "workspace.member.removed",
    1,
    sourceEventId: null,
    WorkspaceId,
    ActorUserId,
    CorrelationId,
    CausationId,
    OccurredAt
);
