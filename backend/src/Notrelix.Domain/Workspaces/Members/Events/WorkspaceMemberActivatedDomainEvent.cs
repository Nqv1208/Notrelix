namespace Notrelix.Domain.Workspaces.Members.Events;

public sealed record WorkspaceMemberActivatedDomainEvent(
    Guid WorkspaceId,
    Guid MemberId,
    Guid UserId,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, ActorId);
