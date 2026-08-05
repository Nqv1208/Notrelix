using Notrelix.Domain.Accounts.Domains.Events;

namespace Notrelix.Domain.Accounts.Domains;

public class AccountDomain : AggregateRoot, IAccountScoped
{
    public Guid AccountId { get; private set; }
    public string Domain { get; private set; } = null!;
    public DomainVerificationStatus VerificationStatus { get; private set; } = DomainVerificationStatus.Pending;
    public string? VerificationTokenHash { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public bool AutoJoinEnabled { get; private set; }

    private AccountDomain() : base() { }

    public static AccountDomain Create(
        Guid accountId,
        string domain,
        Guid createdBy,
        DateTimeOffset createdAt,
        string? verificationTokenHash = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotNullOrWhiteSpace(domain);
        Guard.NotEmpty(createdBy);

        var entity = new AccountDomain
        {
            AccountId = accountId,
            Domain = domain.Trim().ToLowerInvariant(),
            VerificationStatus = DomainVerificationStatus.Pending,
            VerificationTokenHash = verificationTokenHash,
            AutoJoinEnabled = false
        };

        entity.SetAuditOnCreate(createdBy, createdAt);
        entity.RaiseDomainEvent(new AccountDomainCreatedDomainEvent(
            accountId, entity.Id, entity.Domain, createdBy, createdAt));

        return entity;
    }

    public void Verify(DateTimeOffset verifiedAt, Guid verifiedBy)
    {
        if (VerificationStatus == DomainVerificationStatus.Verified) return;
        var pending = PrepareAuditUpdate(verifiedBy, verifiedAt);
        VerificationStatus = DomainVerificationStatus.Verified;
        VerifiedAt = verifiedAt;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AccountDomainVerifiedDomainEvent(AccountId, Id, Domain, verifiedAt));
    }

    public void Reject(Guid rejectedBy, DateTimeOffset rejectedAt)
    {
        if (VerificationStatus == DomainVerificationStatus.Rejected) return;
        var pending = PrepareAuditUpdate(rejectedBy, rejectedAt);
        VerificationStatus = DomainVerificationStatus.Rejected;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AccountDomainRejectedDomainEvent(AccountId, Id, Domain, rejectedAt));
    }

    public void EnableAutoJoin(Guid enabledBy, DateTimeOffset enabledAt)
    {
        if (AutoJoinEnabled) return;
        if (VerificationStatus != DomainVerificationStatus.Verified)
            throw new BusinessRuleException(AccountRuleCodes.Accounts_Domain_CannotEnableAutoJoinUnverified, "Cannot enable auto-join for an unverified domain.");
        var pending = PrepareAuditUpdate(enabledBy, enabledAt);
        AutoJoinEnabled = true;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AccountDomainAutoJoinEnabledDomainEvent(AccountId, Id, Domain, enabledAt));
    }

    public void DisableAutoJoin(Guid disabledBy, DateTimeOffset disabledAt)
    {
        if (!AutoJoinEnabled) return;
        var pending = PrepareAuditUpdate(disabledBy, disabledAt);
        AutoJoinEnabled = false;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new AccountDomainAutoJoinDisabledDomainEvent(AccountId, Id, Domain, disabledAt));
    }
}
