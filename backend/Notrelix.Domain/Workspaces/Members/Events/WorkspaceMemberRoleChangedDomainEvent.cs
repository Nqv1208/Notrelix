using Notrelix.Domain.Common;

namespace Notrelix.Domain.Workspaces.Members.Events;

public sealed record WorkspaceMemberRoleChangedDomainEvent(
    Guid WorkspaceId,
    Guid MemberId,
    Guid UserId,
    WorkspaceRole OldRole,
    WorkspaceRole NewRole,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, ActorId);
