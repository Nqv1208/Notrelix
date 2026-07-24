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

    public void Verify(DateTimeOffset verifiedAt)
    {
        if (VerificationStatus == DomainVerificationStatus.Verified) return;
        VerificationStatus = DomainVerificationStatus.Verified;
        VerifiedAt = verifiedAt;
    }

    public void Reject()
    {
        VerificationStatus = DomainVerificationStatus.Rejected;
    }

    public void EnableAutoJoin()
    {
        if (VerificationStatus != DomainVerificationStatus.Verified)
            throw new BusinessRuleException(BusinessRuleCodes.Accounts_Domain_CannotEnableAutoJoinUnverified, "Cannot enable auto-join for an unverified domain.");
        AutoJoinEnabled = true;
    }

    public void DisableAutoJoin()
    {
        AutoJoinEnabled = false;
    }
}
