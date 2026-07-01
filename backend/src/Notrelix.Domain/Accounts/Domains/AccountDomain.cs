namespace Notrelix.Domain.Accounts.Domains;

public class AccountDomain : AggregateRoot, IAccountScoped
{
    public Guid AccountId { get; private set; }
    public string Domain { get; private set; } = null!;
    public string VerificationStatus { get; private set; } = "Pending";
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
            VerificationStatus = "Pending",
            VerificationTokenHash = verificationTokenHash,
            AutoJoinEnabled = false
        };
    }

    public void Verify(DateTimeOffset verifiedAt)
    {
        if (VerificationStatus == "Verified") return;
        VerificationStatus = "Verified";
        VerifiedAt = verifiedAt;
    }

    public void Reject()
    {
        VerificationStatus = "Rejected";
    }

    public void EnableAutoJoin()
    {
        if (VerificationStatus != "Verified")
            throw new BusinessRuleException("Cannot enable auto-join for an unverified domain.");
        AutoJoinEnabled = true;
    }

    public void DisableAutoJoin()
    {
        AutoJoinEnabled = false;
    }
}
