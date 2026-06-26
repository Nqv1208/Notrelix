namespace Notrelix.Domain.Workspaces.Members.Events;

public sealed record WorkspaceMemberRestoredDomainEvent(
    Guid WorkspaceId,
    Guid MemberId,
    Guid UserId,
    Guid RestoredBy,
    DateTimeOffset RestoredAt
) : DomainEvent(RestoredAt, WorkspaceId, RestoredBy);
