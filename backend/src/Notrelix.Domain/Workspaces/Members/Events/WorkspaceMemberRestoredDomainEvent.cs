namespace Notrelix.Domain.Workspaces.Members.Events;

[EventName("workspaces.workspace-member-restored")]
public sealed record WorkspaceMemberRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid MemberId,
    Guid UserId,
    Guid RestoredBy,
    DateTimeOffset RestoredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, RestoredAt);
