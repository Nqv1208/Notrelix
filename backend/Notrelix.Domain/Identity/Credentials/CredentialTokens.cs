using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Credentials;

public class PasswordResetToken : AggregateRoot
{
    public Guid UserId { get; private set; }
    public TokenHash TokenHash { get; private set; } = null!;
    public CredentialTokenStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    private PasswordResetToken() : base() { }

    public static PasswordResetToken Create(Guid userId, TokenHash hash, DateTimeOffset expiresAt)
    {
        Guard.NotEmpty(userId);
        Guard.NotNull(hash);

        return new PasswordResetToken
        {
            UserId = userId,
            TokenHash = hash,
            Status = CredentialTokenStatus.Active,
            ExpiresAt = expiresAt
        };
    }
}

public class EmailVerificationToken : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string Email { get; private set; } = null!;
    public TokenHash TokenHash { get; private set; } = null!;
    public CredentialTokenStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    private EmailVerificationToken() : base() { }

    public static EmailVerificationToken Create(Guid userId, string email, TokenHash hash, DateTimeOffset expiresAt)
    {
        Guard.NotEmpty(userId);
        Guard.NotNullOrWhiteSpace(email);
        Guard.NotNull(hash);

        return new EmailVerificationToken
        {
            UserId = userId,
            Email = email,
            TokenHash = hash,
            Status = CredentialTokenStatus.Active,
            ExpiresAt = expiresAt
        };
    }
}
