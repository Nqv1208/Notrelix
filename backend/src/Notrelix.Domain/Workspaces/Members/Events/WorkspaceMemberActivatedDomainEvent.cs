namespace Notrelix.Domain.Workspaces.Members.Events;

public sealed record WorkspaceMemberActivatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid MemberId,
    Guid UserId,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, ActorId);
