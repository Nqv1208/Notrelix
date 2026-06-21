using Notrelix.Domain.Common;
using Notrelix.Application.Common.Events;

namespace Notrelix.Application.Events.Workspaces;

[EventName("workspace.member.added", Version = 1)]
public sealed record WorkspaceMemberAddedIntegrationEvent(
    Guid? WorkspaceId,
    Guid UserId,
    string Role,
    Guid? ActorUserId = null,
    string? CorrelationId = null,
    string? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "workspace.member.added",
    1,
    sourceEventId: null,
    WorkspaceId,
    ActorUserId,
    CorrelationId,
    CausationId,
    OccurredAt
);
