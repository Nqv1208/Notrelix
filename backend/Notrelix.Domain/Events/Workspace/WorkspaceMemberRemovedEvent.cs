using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Events.Workspace;

public class WorkspaceMemberRemovedEvent : BaseEvent
{
    public Guid WorkspaceId { get; }
    public Guid UserId { get; }
    public WorkspaceRole OldRole { get; }
    public Guid RemovedBy { get; }

    public WorkspaceMemberRemovedEvent(Guid workspaceId, Guid userId, WorkspaceRole oldRole, Guid removedBy)
    {
        WorkspaceId = workspaceId;
        UserId = userId;
        OldRole = oldRole;
        RemovedBy = removedBy;
    }
}
