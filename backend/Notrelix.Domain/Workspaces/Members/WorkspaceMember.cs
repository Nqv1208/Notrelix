using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Workspaces.Members;

public class WorkspaceMember : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public WorkspaceRole Role { get; private set; }
    public WorkspaceMemberStatus Status { get; private set; }

    private WorkspaceMember() : base() { }

    internal static WorkspaceMember Create(Guid workspaceId, Guid userId, WorkspaceRole role, Guid addedBy)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(userId);

        var member = new WorkspaceMember
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            Role = role,
            Status = WorkspaceMemberStatus.Active
        };

        member.SetAuditOnCreate(addedBy);
        return member;
    }

    internal void ChangeRole(WorkspaceRole newRole, Guid updatedBy)
    {
        if (Role == newRole) return;
        Role = newRole;
        SetAuditOnUpdate(updatedBy);
    }

    public void Suspend(Guid updatedBy)
    {
        if (Status == WorkspaceMemberStatus.Suspended) return;
        Status = WorkspaceMemberStatus.Suspended;
        SetAuditOnUpdate(updatedBy);
    }

    public void Activate(Guid updatedBy)
    {
        if (Status == WorkspaceMemberStatus.Active) return;
        Status = WorkspaceMemberStatus.Active;
        SetAuditOnUpdate(updatedBy);
    }
}
