namespace Notrelix.Application.Events.Workspaces;

[EventName("workspace.created", Version = 1)]
public sealed record WorkspaceCreatedIntegrationEvent(
    Guid? WorkspaceId,
    string Name,
    string Slug,
    Guid OwnerId,
    Guid? ActorUserId = null,
    Guid CorrelationId = default,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "workspace.created",
    1,
    sourceEventId: null,
    accountId: null,
    WorkspaceId,
    ActorUserId,
    CorrelationId,
    CausationId,
    OccurredAt
);
