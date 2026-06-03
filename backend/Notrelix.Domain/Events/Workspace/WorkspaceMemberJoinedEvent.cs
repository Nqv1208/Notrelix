using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Events.Workspace;

public class WorkspaceMemberJoinedEvent : BaseEvent
{
    public Guid WorkspaceId { get; }
    public Guid UserId { get; }
    public WorkspaceRole Role { get; }
    public Guid? InvitedBy { get; }

    public WorkspaceMemberJoinedEvent(Guid workspaceId, Guid userId, WorkspaceRole role, Guid? invitedBy)
    {
        WorkspaceId = workspaceId;
        UserId = userId;
        Role = role;
        InvitedBy = invitedBy;
    }
}
