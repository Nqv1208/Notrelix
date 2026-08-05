using Notrelix.Domain.Accounts.Members.Events;
using Notrelix.Domain.Accounts.Rules;

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
            throw new BusinessRuleException(AccountRuleCodes.Accounts_Member_CannotChangeRoleOfInactive, "Cannot change role of an inactive or suspended member.");

        AccountOwnerRules.EnsureCanDowngradeOwner(Role, newRole, activeOwnerCount);

        if (Role == newRole) return;

        var oldRole = Role;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Role = newRole;
        ApplyAuditUpdate(pending);
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

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Status = AccountMemberStatus.Suspended;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AccountMemberSuspendedDomainEvent(AccountId, Id, UserId, updatedBy, updatedAt));
    }

    public void Activate(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);

        if (Status == AccountMemberStatus.Active) return;

        if (Status == AccountMemberStatus.Removed)
            throw new BusinessRuleException(AccountRuleCodes.Accounts_Member_CannotActivateRemoved, "Cannot activate a removed member. Restore the member first.");

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Status = AccountMemberStatus.Active;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AccountMemberActivatedDomainEvent(AccountId, Id, UserId, updatedBy, updatedAt));
    }

    private void Delete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new AccountMemberRemovedDomainEvent(AccountId, Id, UserId, deletedBy, deletedAt, pendingDeletion.Reason));
    }

    public void Remove(int activeOwnerCount, Guid removedBy, DateTimeOffset removedAt, string? reason = null)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(removedBy);

        AccountOwnerRules.EnsureCanRemoveOwner(Role, activeOwnerCount);

        Delete(removedBy, removedAt, reason);
    }

    private void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        IncrementVersion();
        RaiseDomainEvent(new AccountMemberRestoredDomainEvent(AccountId, Id, UserId, restoredBy, restoredAt));
    }
}
