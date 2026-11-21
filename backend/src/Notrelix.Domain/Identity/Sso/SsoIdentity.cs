namespace Notrelix.Domain.Identity.Sso;

/// <summary>
/// A person's sign-in identity at one Account's company identity provider: the provider's subject for the person,
/// bound to exactly one user. One row per (user, Account) and one per (Account, subject).
/// </summary>
public sealed class SsoIdentity : Entity
{
    public Guid AccountId { get; private set; }
    public string Issuer { get; private set; } = null!;
    public string Subject { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private SsoIdentity() : base() { }

    public static SsoIdentity Create(Guid accountId, string issuer, string subject, Guid userId, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotNullOrWhiteSpace(issuer);
        Guard.NotNullOrWhiteSpace(subject);
        Guard.NotEmpty(userId);

        return new SsoIdentity
        {
            AccountId = accountId,
            Issuer = issuer.Trim(),
            Subject = subject.Trim(),
            UserId = userId,
            CreatedAt = createdAt,
        };
    }
}
