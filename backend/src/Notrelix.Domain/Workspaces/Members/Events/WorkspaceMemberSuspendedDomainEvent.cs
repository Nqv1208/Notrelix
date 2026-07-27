namespace Notrelix.Domain.Workspaces.Members.Events;

[EventName("workspaces.workspace-member-suspended")]
public sealed record WorkspaceMemberSuspendedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid MemberId,
    Guid UserId,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
