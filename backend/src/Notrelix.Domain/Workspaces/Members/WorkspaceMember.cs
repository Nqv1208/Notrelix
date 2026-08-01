using Notrelix.Domain.Workspaces.Members.Events;
using Notrelix.Domain.Workspaces.Rules;
namespace Notrelix.Domain.Workspaces.Members;

public class WorkspaceMember : AggregateRoot, IWorkspaceScoped
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

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);
        var oldRole = Role;
        Role = newRole;

        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceMemberRoleChangedDomainEvent(
            AccountId, WorkspaceId, Id, UserId, oldRole, newRole, updatedBy, updatedAt));
    }

    public void PromoteToOwner(Guid promotedBy, DateTimeOffset promotedAt)
    {
        Guard.NotEmpty(promotedBy);

        if (Status != WorkspaceMemberStatus.Active)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Member_CannotPromoteInactiveToOwner, "Cannot promote an inactive member to owner.");

        if (Role == WorkspaceRole.Owner) return;

        var audit = PrepareAuditUpdate(promotedBy, promotedAt);
        var oldRole = Role;
        Role = WorkspaceRole.Owner;

        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceMemberRoleChangedDomainEvent(
            AccountId, WorkspaceId, Id, UserId, oldRole, WorkspaceRole.Owner, promotedBy, promotedAt));
    }

    public void Suspend(
        Guid updatedBy,
        DateTimeOffset updatedAt,
        int activeOwnerCount)
    {
        Guard.NotEmpty(updatedBy);

        if (Status == WorkspaceMemberStatus.Removed)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Member_CannotSuspendRemoved, "Cannot suspend a removed member.");

        if (Status == WorkspaceMemberStatus.Suspended) return;

        WorkspaceOwnerRules.EnsureCanSuspendOwner(Role, activeOwnerCount);

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);
        Status = WorkspaceMemberStatus.Suspended;

        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceMemberSuspendedDomainEvent(
            AccountId, WorkspaceId, Id, UserId, updatedBy, updatedAt));
    }

    public void Activate(Guid updatedBy, DateTimeOffset updatedAt)
    {
        Guard.NotEmpty(updatedBy);

        if (Status == WorkspaceMemberStatus.Removed)
        {
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Member_CannotActivateRemoved, "Cannot activate a removed member.");
        }

        if (Status == WorkspaceMemberStatus.Active) return;

        var audit = PrepareAuditUpdate(updatedBy, updatedAt);
        Status = WorkspaceMemberStatus.Active;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceMemberActivatedDomainEvent(AccountId, WorkspaceId, Id, UserId, updatedBy, updatedAt));
    }

    public void Remove(int activeOwnerCount, Guid removedBy, DateTimeOffset removedAt, string? reason = null)
    {
        Guard.NotEmpty(removedBy);

        if (Status == WorkspaceMemberStatus.Removed) return;

        WorkspaceOwnerRules.EnsureCanRemoveOwner(Role, activeOwnerCount);

        var audit = PrepareAuditUpdate(removedBy, removedAt);
        Status = WorkspaceMemberStatus.Removed;
        ApplyAuditUpdate(audit);
        IncrementVersion();
        RaiseDomainEvent(new WorkspaceMemberRemovedDomainEvent(AccountId, WorkspaceId, Id, UserId, removedBy, removedAt));
    }
}
