using Notrelix.Domain.Accounts.Accounts.Events;
namespace Notrelix.Domain.Accounts.Accounts;

public class Account : SoftDeletableAggregateRoot, IAccountScoped
{
    public Guid AccountId => Id;
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? LegalName { get; private set; }
    public AccountStatus Status { get; private set; }
    public AccountType Type { get; private set; }
    public string? DefaultRegionCode { get; private set; }
    public string? PlanCode { get; private set; }

    private Account() : base() { }

    public static Account Create(
        string name,
        string slug,
        AccountType type,
        Guid createdBy,
        DateTimeOffset createdAt,
        string? legalName = null)
    {
        Guard.NotNullOrWhiteSpace(name);
        Guard.MaxLength(name, 160);
        Guard.NotNullOrWhiteSpace(slug);

        var slugValue = SharedKernel.Slug.Create(slug);

        var account = new Account
        {
            Name = name.Trim(),
            Slug = slugValue.Value,
            LegalName = legalName?.Trim(),
            Status = AccountStatus.Active,
            Type = type
        };

        account.SetAuditOnCreate(createdBy, createdAt);
        account.RaiseDomainEvent(new AccountCreatedDomainEvent(
            account.Id, account.Name, account.Slug, account.Type, createdBy, createdAt));

        return account;
    }

    public void Rename(string newName, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNullOrWhiteSpace(newName);
        Guard.MaxLength(newName, 160);

        if (Status == AccountStatus.Closed)
            throw new BusinessRuleException(AccountRuleCodes.Accounts_Account_CannotRenameClosed, "Cannot rename a closed account.");

        var oldName = Name;
        if (Name == newName.Trim()) return;

        Name = newName.Trim();
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new AccountRenamedDomainEvent(Id, oldName, Name, updatedBy, updatedAt));
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        if (Status == AccountStatus.Closed) return;

        Status = AccountStatus.Closed;
        SetAuditOnUpdate(archivedBy, archivedAt);
        IncrementVersion();
        RaiseDomainEvent(new AccountArchivedDomainEvent(Id, archivedBy, archivedAt));
    }

    public void Suspend(Guid suspendedBy, DateTimeOffset suspendedAt, string? reason = null)
    {
        EnsureNotDeleted();
        if (Status == AccountStatus.Suspended) return;

        var previousStatus = Status;
        Status = AccountStatus.Suspended;
        SetAuditOnUpdate(suspendedBy, suspendedAt);
        IncrementVersion();
        RaiseDomainEvent(new AccountSuspendedDomainEvent(Id, previousStatus, suspendedBy, suspendedAt, reason));
    }

    public void Activate(Guid activatedBy, DateTimeOffset activatedAt)
    {
        EnsureNotDeleted();
        if (Status == AccountStatus.Active || Status == AccountStatus.Trialing) return;

        var previousStatus = Status;
        Status = AccountStatus.Active;
        SetAuditOnUpdate(activatedBy, activatedAt);
        IncrementVersion();
        RaiseDomainEvent(new AccountActivatedDomainEvent(Id, previousStatus, activatedBy, activatedAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        Status = AccountStatus.SoftDeleted;
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        RaiseDomainEvent(new AccountSoftDeletedDomainEvent(Id, deletedBy, deletedAt, reason));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        Status = AccountStatus.Active;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new AccountRestoredDomainEvent(Id, restoredBy, restoredAt));
    }

    public void UpdatePlanCode(string? planCode, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        var normalized = planCode?.Trim();
        if (PlanCode == normalized) return;

        var oldPlanCode = PlanCode;
        PlanCode = normalized;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new AccountPlanCodeChangedDomainEvent(Id, oldPlanCode, PlanCode, updatedBy, updatedAt));
    }

    public void UpdateDefaultRegion(string? regionCode, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        var normalized = regionCode?.Trim();
        if (DefaultRegionCode == normalized) return;

        var oldRegionCode = DefaultRegionCode;
        DefaultRegionCode = normalized;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new AccountDefaultRegionChangedDomainEvent(Id, oldRegionCode, DefaultRegionCode, updatedBy, updatedAt));
    }
}
