namespace Notrelix.Domain.Workspaces.Members.Events;

public sealed record WorkspaceMemberRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid MemberId,
    Guid UserId,
    Guid RestoredBy,
    DateTimeOffset RestoredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, RestoredAt, RestoredBy);
