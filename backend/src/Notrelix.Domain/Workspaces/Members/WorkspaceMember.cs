using Notrelix.Domain.Workspaces.Members.Events;
using Notrelix.Domain.Workspaces.Rules;
namespace Notrelix.Domain.Workspaces.Members;

public class WorkspaceMember : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public WorkspaceRole Role { get; private set; }
    public WorkspaceMemberStatus Status { get; private set; }

    private WorkspaceMember() : base() { }

    public static WorkspaceMember Create(Guid accountId, Guid workspaceId, Guid userId, WorkspaceRole role, Guid addedBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(userId);
        Guard.NotEmpty(addedBy);

        var member = new WorkspaceMember
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            UserId = userId,
            Role = role,
            Status = WorkspaceMemberStatus.Active
        };

        member.SetAuditOnCreate(addedBy, createdAt);
        member.RaiseDomainEvent(new WorkspaceMemberAddedDomainEvent(accountId, workspaceId, member.Id, userId, role, addedBy, createdAt));
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

        if (newRole == WorkspaceRole.Owner)
        {
            throw new BusinessRuleException(
                WorkspaceRuleCodes.Workspaces_Member_CannotDirectlyAssignOwner,
                "Ownership must be transferred through the ownership transfer workflow.");
        }

        if (Status != WorkspaceMemberStatus.Active)
        {
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Member_CannotChangeRoleOfInactive, "Cannot change role of an inactive or suspended member.");
        }

        WorkspaceOwnerRules.EnsureCanDowngradeOwner(Role, newRole, activeOwnerCount);

        if (Role == newRole) return;

        var oldRole = Role;
        Role = newRole;

        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceMemberRoleChangedDomainEvent(
            AccountId, WorkspaceId, Id, UserId, oldRole, newRole, updatedBy, updatedAt));
    }

    public void PromoteToOwner(Guid promotedBy, DateTimeOffset promotedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(promotedBy);

        if (Status != WorkspaceMemberStatus.Active)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Member_CannotPromoteInactiveToOwner, "Cannot promote an inactive member to owner.");

        if (Role == WorkspaceRole.Owner) return;

        var oldRole = Role;
        Role = WorkspaceRole.Owner;

        SetAuditOnUpdate(promotedBy, promotedAt);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceMemberRoleChangedDomainEvent(
            AccountId, WorkspaceId, Id, UserId, oldRole, WorkspaceRole.Owner, promotedBy, promotedAt));
    }

    public void Suspend(
        Guid updatedBy,
        DateTimeOffset updatedAt,
        int activeOwnerCount)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        if (Status == WorkspaceMemberStatus.Suspended) return;

        WorkspaceOwnerRules.EnsureCanSuspendOwner(Role, activeOwnerCount);

        Status = WorkspaceMemberStatus.Suspended;

        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceMemberSuspendedDomainEvent(
            AccountId, WorkspaceId, Id, UserId, updatedBy, updatedAt));
    }

    public void Activate(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        if (Status == WorkspaceMemberStatus.Active) return;

        if (Status == WorkspaceMemberStatus.Removed)
        {
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Member_CannotActivateRemoved, "Cannot activate a removed member. Restore the member first.");
        }

        Status = WorkspaceMemberStatus.Active;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceMemberActivatedDomainEvent(AccountId, WorkspaceId, Id, UserId, updatedBy, updatedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);

        if (IsDeleted) return;

        Status = WorkspaceMemberStatus.Removed;
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceMemberRemovedDomainEvent(AccountId, WorkspaceId, Id, UserId, deletedBy, deletedAt));
    }

    public void Remove(int activeOwnerCount, Guid removedBy, DateTimeOffset removedAt, string? reason = null)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(removedBy);

        WorkspaceOwnerRules.EnsureCanRemoveOwner(Role, activeOwnerCount);

        SoftDelete(removedBy, removedAt, reason);
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;

        Guard.NotEmpty(restoredBy);

        Status = WorkspaceMemberStatus.Active;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceMemberRestoredDomainEvent(
            AccountId,
            WorkspaceId,
            Id,
            UserId,
            restoredBy,
            restoredAt));
    }
}
