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

    public static WorkspaceMember Create(Guid workspaceId, Guid userId, WorkspaceRole role, Guid addedBy, DateTimeOffset createdAt)
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

        member.SetAuditOnCreate(addedBy, createdAt);
        member.AddDomainEvent(new WorkspaceMemberAddedEvent(workspaceId, member.Id, userId, role, addedBy, createdAt));
        return member;
    }

    public void ChangeRole(WorkspaceRole newRole, Guid updatedBy, DateTimeOffset updatedAt)
    {
        if (Role == newRole) return;
        var oldRole = Role;
        Role = newRole;
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new WorkspaceMemberRoleChangedEvent(WorkspaceId, Id, oldRole, newRole, updatedBy, updatedAt));
    }

    public void Suspend(Guid updatedBy, DateTimeOffset updatedAt)
    {
        if (Status == WorkspaceMemberStatus.Suspended) return;
        Status = WorkspaceMemberStatus.Suspended;
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new WorkspaceMemberSuspendedEvent(WorkspaceId, Id, updatedBy, updatedAt));
    }

    public void Activate(Guid updatedBy, DateTimeOffset updatedAt)
    {
        if (Status == WorkspaceMemberStatus.Active) return;
        Status = WorkspaceMemberStatus.Active;
        SetAuditOnUpdate(updatedBy, updatedAt);
        AddDomainEvent(new WorkspaceMemberActivatedEvent(WorkspaceId, Id, updatedBy, updatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        AddDomainEvent(new WorkspaceMemberRemovedEvent(WorkspaceId, Id, deletedBy, deletedAt));
    }
}
