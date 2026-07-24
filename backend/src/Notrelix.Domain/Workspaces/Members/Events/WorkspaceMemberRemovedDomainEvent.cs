namespace Notrelix.Domain.Workspaces.Members.Events;

[EventName("workspaces.workspace-member-removed")]
public sealed record WorkspaceMemberRemovedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid MemberId,
    Guid UserId,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
