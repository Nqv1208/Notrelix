using Notrelix.Application.Common.Events;

namespace Notrelix.Application.Events.Workspaces;

[EventName("workspace.created", Version = 1)]
public sealed record WorkspaceCreatedIntegrationEvent(
    Guid? WorkspaceId,
    string Name,
    string Slug,
    Guid OwnerId,
    Guid? ActorUserId = null,
    string? CorrelationId = null,
    string? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "workspace.created",
    1,
    sourceEventId: null,
    WorkspaceId,
    ActorUserId,
    CorrelationId,
    CausationId,
    OccurredAt
);
