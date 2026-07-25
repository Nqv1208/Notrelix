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

    public static AccountDomain Create(Guid accountId, string domain, string? verificationTokenHash = null)
    {
        Guard.NotEmpty(accountId);
        Guard.NotNullOrWhiteSpace(domain);

        return new AccountDomain
        {
            AccountId = accountId,
            Domain = domain.Trim().ToLowerInvariant(),
            VerificationStatus = DomainVerificationStatus.Pending,
            VerificationTokenHash = verificationTokenHash,
            AutoJoinEnabled = false
        };
    }

    public void Verify(DateTimeOffset verifiedAt, Guid verifiedBy)
    {
        if (VerificationStatus == DomainVerificationStatus.Verified) return;
        VerificationStatus = DomainVerificationStatus.Verified;
        VerifiedAt = verifiedAt;
        SetAuditOnUpdate(verifiedBy, verifiedAt);
        IncrementVersion();
        RaiseDomainEvent(new AccountDomainVerifiedDomainEvent(AccountId, Id, Domain, verifiedAt));
    }

    public void Reject(Guid rejectedBy, DateTimeOffset rejectedAt)
    {
        if (VerificationStatus == DomainVerificationStatus.Rejected) return;
        VerificationStatus = DomainVerificationStatus.Rejected;
        SetAuditOnUpdate(rejectedBy, rejectedAt);
        IncrementVersion();
        RaiseDomainEvent(new AccountDomainRejectedDomainEvent(AccountId, Id, Domain, rejectedAt));
    }

    public void EnableAutoJoin(Guid enabledBy, DateTimeOffset enabledAt)
    {
        if (AutoJoinEnabled) return;
        if (VerificationStatus != DomainVerificationStatus.Verified)
            throw new BusinessRuleException(AccountRuleCodes.Accounts_Domain_CannotEnableAutoJoinUnverified, "Cannot enable auto-join for an unverified domain.");
        AutoJoinEnabled = true;
        SetAuditOnUpdate(enabledBy, enabledAt);
        IncrementVersion();
        RaiseDomainEvent(new AccountDomainAutoJoinEnabledDomainEvent(AccountId, Id, Domain, enabledAt));
    }

    public void DisableAutoJoin(Guid disabledBy, DateTimeOffset disabledAt)
    {
        if (!AutoJoinEnabled) return;
        AutoJoinEnabled = false;
        SetAuditOnUpdate(disabledBy, disabledAt);
        IncrementVersion();
        RaiseDomainEvent(new AccountDomainAutoJoinDisabledDomainEvent(AccountId, Id, Domain, disabledAt));
    }
}
