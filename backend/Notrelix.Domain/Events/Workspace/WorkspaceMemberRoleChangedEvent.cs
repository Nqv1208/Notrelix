using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Events.Workspace;

public class WorkspaceMemberRoleChangedEvent : BaseEvent
{
    public Guid WorkspaceId { get; }
    public Guid UserId { get; }
    public WorkspaceRole OldRole { get; }
    public WorkspaceRole NewRole { get; }
    public Guid ChangedBy { get; }

    public WorkspaceMemberRoleChangedEvent(Guid workspaceId, Guid userId, WorkspaceRole oldRole, WorkspaceRole newRole, Guid changedBy)
    {
        WorkspaceId = workspaceId;
        UserId = userId;
        OldRole = oldRole;
        NewRole = newRole;
        ChangedBy = changedBy;
    }
}
