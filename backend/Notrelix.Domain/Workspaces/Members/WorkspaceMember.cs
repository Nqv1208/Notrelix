using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Workspaces.Members.Events;

namespace Notrelix.Domain.Workspaces.Members;

public class WorkspaceMember : AggregateRoot, IWorkspaceScoped
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
        Guard.NotEmpty(addedBy);

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

    public void ChangeRole(
        WorkspaceRole newRole,
        Guid updatedBy,
        int activeOwnerCount,
        DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        
        if (Status != WorkspaceMemberStatus.Active)
        {
            throw new BusinessRuleException("Cannot change role of an inactive or suspended member.");
        }

        WorkspaceOwnerRules.EnsureCanDowngradeOwner(Role, newRole, activeOwnerCount);
        
        if (Role == newRole) return;
        
        var oldRole = Role;
        Role = newRole;

        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new WorkspaceMemberRoleChangedEvent(
            WorkspaceId, Id, UserId, oldRole, newRole, updatedBy, updatedAt));
    }

    public void Suspend(
        Guid updatedBy,
        DateTimeOffset updatedAt,
        int activeOwnerCount)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        
        WorkspaceOwnerRules.EnsureCanSuspendOwner(Role, activeOwnerCount);

        if (Status == WorkspaceMemberStatus.Suspended) return;
        
        Status = WorkspaceMemberStatus.Suspended;

        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new WorkspaceMemberSuspendedEvent(
            WorkspaceId, Id, UserId, updatedBy, updatedAt));
    }

    public void Activate(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        
        if (Status == WorkspaceMemberStatus.Active) return;
        
        if (Status == WorkspaceMemberStatus.Removed)
        {
            throw new BusinessRuleException("Cannot activate a removed member. Restore the member first.");
        }
        
        Status = WorkspaceMemberStatus.Active;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        AddDomainEvent(new WorkspaceMemberActivatedEvent(WorkspaceId, Id, UserId, updatedBy, updatedAt));
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);

        if (IsDeleted) return;
        
        Status = WorkspaceMemberStatus.Removed;
        base.SoftDelete(deletedBy, deletedAt, reason);
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        AddDomainEvent(new WorkspaceMemberRemovedEvent(WorkspaceId, Id, UserId, deletedBy, deletedAt));
    }

    public void Remove(int activeOwnerCount, Guid removedBy, DateTimeOffset removedAt, string? reason = null)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(removedBy);

        WorkspaceOwnerRules.EnsureCanRemoveOwner(Role, activeOwnerCount);

        SoftDelete(removedBy, removedAt, reason);
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;

        Guard.NotEmpty(restoredBy);

        Status = WorkspaceMemberStatus.Active;
        base.Restore(restoredBy, restoredAt);
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        AddDomainEvent(new WorkspaceMemberRestoredEvent(
            WorkspaceId,
            Id,
            UserId,
            restoredBy,
            restoredAt));
    }
}
