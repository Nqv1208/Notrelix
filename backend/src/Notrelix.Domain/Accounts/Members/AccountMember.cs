using Notrelix.Domain.Accounts.Members.Events;
using Notrelix.Domain.Accounts.Rules;
using Notrelix.Domain.Workspaces;
namespace Notrelix.Domain.Accounts.Members;

public class AccountMember : SoftDeletableAggregateRoot, IAccountScoped
{
    public Guid AccountId { get; private set; }
    public Guid UserId { get; private set; }
    public AccountRole Role { get; private set; }
    public AccountMemberStatus Status { get; private set; }

    private AccountMember() : base() { }

    public static AccountMember Create(Guid accountId, Guid userId, AccountRole role, Guid addedBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(userId);
        Guard.NotEmpty(addedBy);

        var member = new AccountMember
        {
            AccountId = accountId,
            UserId = userId,
            Role = role,
            Status = AccountMemberStatus.Active
        };

        member.SetAuditOnCreate(addedBy, createdAt);
        member.RaiseDomainEvent(new AccountMemberAddedDomainEvent(accountId, member.Id, userId, role, addedBy, createdAt));
        return member;
    }

    public void ChangeRole(AccountRole newRole, Guid updatedBy, int activeOwnerCount, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        if (Status != AccountMemberStatus.Active)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Member_CannotChangeRoleOfInactive, "Cannot change role of an inactive or suspended member.");

        AccountOwnerRules.EnsureCanDowngradeOwner(Role, newRole, activeOwnerCount);

        if (Role == newRole) return;

        var oldRole = Role;
        Role = newRole;

        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new AccountMemberRoleChangedDomainEvent(
            AccountId, Id, UserId, oldRole, newRole, updatedBy, updatedAt));
    }

    public void Suspend(Guid updatedBy, DateTimeOffset updatedAt, int activeOwnerCount)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        AccountOwnerRules.EnsureCanSuspendOwner(Role, activeOwnerCount);

        if (Status == AccountMemberStatus.Suspended) return;

        Status = AccountMemberStatus.Suspended;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new AccountMemberSuspendedDomainEvent(AccountId, Id, UserId, updatedBy, updatedAt));
    }

    public void Activate(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        if (Status == AccountMemberStatus.Active) return;

        if (Status == AccountMemberStatus.Removed)
            throw new BusinessRuleException(WorkspaceRuleCodes.Workspaces_Member_CannotActivateRemoved, "Cannot activate a removed member. Restore the member first.");

        Status = AccountMemberStatus.Active;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new AccountMemberActivatedDomainEvent(AccountId, Id, UserId, updatedBy, updatedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);

        if (IsDeleted) return;

        Status = AccountMemberStatus.Removed;
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        RaiseDomainEvent(new AccountMemberRemovedDomainEvent(AccountId, Id, UserId, deletedBy, deletedAt));
    }

    public void Remove(int activeOwnerCount, Guid removedBy, DateTimeOffset removedAt, string? reason = null)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(removedBy);

        AccountOwnerRules.EnsureCanRemoveOwner(Role, activeOwnerCount);

        SoftDelete(removedBy, removedAt, reason);
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;

        Guard.NotEmpty(restoredBy);

        Status = AccountMemberStatus.Active;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new AccountMemberRestoredDomainEvent(AccountId, Id, UserId, restoredBy, restoredAt));
    }
}
