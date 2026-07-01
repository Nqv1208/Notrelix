namespace Notrelix.Domain.Workspaces.Members.Events;

public sealed record WorkspaceMemberAddedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid MemberId,
    Guid UserId,
    WorkspaceRole Role,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, ActorId);
