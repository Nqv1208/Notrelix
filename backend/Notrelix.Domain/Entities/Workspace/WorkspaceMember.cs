using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Entities.Workspace;

// Entity đại diện cho thành viên trong workspace
public class WorkspaceMember : BaseEntity
{
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public WorkspaceRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public Guid? InvitedBy { get; private set; }

    // Navigation
    public Workspace Workspace { get; private set; } = null!;

    private WorkspaceMember() : base() { }

    public static WorkspaceMember Create(Guid workspaceId, Guid userId, WorkspaceRole role = WorkspaceRole.Member, Guid? invitedBy = null)
    {
        return new WorkspaceMember
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow,
            InvitedBy = invitedBy
        };
    }

    public void UpdateRole(WorkspaceRole newRole)
    {
        Role = newRole;
    }

    public bool IsOwner => Role == WorkspaceRole.Owner;
    public bool IsAdmin => Role is WorkspaceRole.Owner or WorkspaceRole.Admin;
}
