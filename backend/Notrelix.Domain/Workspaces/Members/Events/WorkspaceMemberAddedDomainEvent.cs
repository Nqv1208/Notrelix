using Notrelix.Domain.Common;

namespace Notrelix.Domain.Workspaces.Members.Events;

public sealed record WorkspaceMemberAddedDomainEvent(
    Guid WorkspaceId,
    Guid MemberId,
    Guid UserId,
    WorkspaceRole Role,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, ActorId);
