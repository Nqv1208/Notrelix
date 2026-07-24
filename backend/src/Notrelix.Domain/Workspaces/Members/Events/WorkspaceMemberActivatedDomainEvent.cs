namespace Notrelix.Domain.Workspaces.Members.Events;

[EventName("workspaces.workspace-member-activated")]
public sealed record WorkspaceMemberActivatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid MemberId,
    Guid UserId,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
