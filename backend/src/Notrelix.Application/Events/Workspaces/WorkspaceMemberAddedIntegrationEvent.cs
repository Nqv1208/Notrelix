namespace Notrelix.Application.Events.Workspaces;

[EventName("workspace.member.added", Version = 1)]
public sealed record WorkspaceMemberAddedIntegrationEvent(
    Guid? WorkspaceId,
    Guid UserId,
    string Role,
    Guid? ActorUserId = null,
    Guid CorrelationId = default,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "workspace.member.added",
    1,
    sourceEventId: null,
    accountId: null,
    WorkspaceId,
    ActorUserId,
    CorrelationId,
    CausationId,
    OccurredAt
);
