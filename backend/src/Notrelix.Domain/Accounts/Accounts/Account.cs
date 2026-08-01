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
        Guard.NotEmpty(updatedBy);
        Guard.NotNullOrWhiteSpace(newName);
        Guard.MaxLength(newName, 160);

        if (Status == AccountStatus.Closed)
            throw new BusinessRuleException(AccountRuleCodes.Accounts_Account_CannotRenameClosed, "Cannot rename a closed account.");

        var oldName = Name;
        if (Name == newName.Trim()) return;

        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        Name = newName.Trim();
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AccountRenamedDomainEvent(Id, oldName, Name, updatedBy, updatedAt));
    }

    public void Archive(Guid archivedBy, DateTimeOffset archivedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(archivedBy);
        if (Status == AccountStatus.Closed) return;

        var pending = PrepareAuditUpdate(archivedBy, archivedAt);
        Status = AccountStatus.Closed;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AccountArchivedDomainEvent(Id, archivedBy, archivedAt));
    }

    public void Suspend(Guid suspendedBy, DateTimeOffset suspendedAt, string? reason = null)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(suspendedBy);
        if (Status == AccountStatus.Suspended) return;

        var previousStatus = Status;
        var pending = PrepareAuditUpdate(suspendedBy, suspendedAt);
        Status = AccountStatus.Suspended;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AccountSuspendedDomainEvent(Id, previousStatus, suspendedBy, suspendedAt, reason));
    }

    public void Activate(Guid activatedBy, DateTimeOffset activatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(activatedBy);
        if (Status == AccountStatus.Active || Status == AccountStatus.Trialing) return;

        var previousStatus = Status;
        var pending = PrepareAuditUpdate(activatedBy, activatedAt);
        Status = AccountStatus.Active;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AccountActivatedDomainEvent(Id, previousStatus, activatedBy, activatedAt));
    }

    public void Delete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new AccountDeletedDomainEvent(Id, Status, deletedBy, deletedAt, pendingDeletion.Reason));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        IncrementVersion();
        RaiseDomainEvent(new AccountRestoredDomainEvent(Id, Status, restoredBy, restoredAt));
    }

    public void UpdatePlanCode(string? planCode, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        var normalized = planCode?.Trim();
        if (PlanCode == normalized) return;

        var oldPlanCode = PlanCode;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        PlanCode = normalized;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AccountPlanCodeChangedDomainEvent(Id, oldPlanCode, PlanCode, updatedBy, updatedAt));
    }

    public void UpdateDefaultRegion(string? regionCode, Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotEmpty(updatedBy);
        var normalized = regionCode?.Trim();
        if (DefaultRegionCode == normalized) return;

        var oldRegionCode = DefaultRegionCode;
        var pending = PrepareAuditUpdate(updatedBy, updatedAt);
        DefaultRegionCode = normalized;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AccountDefaultRegionChangedDomainEvent(Id, oldRegionCode, DefaultRegionCode, updatedBy, updatedAt));
    }
}
