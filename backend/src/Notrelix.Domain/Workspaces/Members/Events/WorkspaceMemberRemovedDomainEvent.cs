namespace Notrelix.Domain.Workspaces.Members.Events;

public sealed record WorkspaceMemberRemovedDomainEvent(
    Guid WorkspaceId,
    Guid MemberId,
    Guid UserId,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, ActorId);
