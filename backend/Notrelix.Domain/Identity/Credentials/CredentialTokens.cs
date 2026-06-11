using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Identity.Credentials;

public class PasswordResetToken : AggregateRoot
{
    public Guid UserId { get; private set; }
    public TokenHash TokenHash { get; private set; } = null!;
    public CredentialTokenStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }

    private PasswordResetToken() : base() { }

    public static PasswordResetToken Create(Guid userId, TokenHash hash, DateTimeOffset expiresAt, DateTimeOffset createdAt, string? ipAddress = null, string? userAgent = null)
    {
        Guard.NotEmpty(userId);
        Guard.NotNull(hash);

        var token = new PasswordResetToken
        {
            UserId = userId,
            TokenHash = hash,
            Status = CredentialTokenStatus.Active,
            ExpiresAt = expiresAt,
            IpAddress = ipAddress,
            UserAgent = userAgent
        };

        token.SetAuditOnCreate(userId, createdAt);
        token.AddDomainEvent(new PasswordResetRequestedEvent(userId, string.Empty, createdAt));
        return token;
    }

    public void Consume(DateTimeOffset consumedAt)
    {
        if (Status != CredentialTokenStatus.Active || consumedAt > ExpiresAt)
        {
            Status = CredentialTokenStatus.Expired;
            throw new DomainException("Password reset token is already consumed or expired.");
        }

        Status = CredentialTokenStatus.Consumed;
        ConsumedAt = consumedAt;
        SetAuditOnUpdate(UserId, consumedAt);
        AddDomainEvent(new PasswordResetCompletedEvent(UserId, consumedAt));
    }

    public void Expire()
    {
        if (Status != CredentialTokenStatus.Active) return;
        Status = CredentialTokenStatus.Expired;
    }
}

public class EmailVerificationToken : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string Email { get; private set; } = null!;
    public TokenHash TokenHash { get; private set; } = null!;
    public CredentialTokenStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }

    private EmailVerificationToken() : base() { }

    public static EmailVerificationToken Create(Guid userId, string email, TokenHash hash, DateTimeOffset expiresAt, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(userId);
        Guard.NotNullOrWhiteSpace(email);
        Guard.NotNull(hash);

        var token = new EmailVerificationToken
        {
            UserId = userId,
            Email = email,
            TokenHash = hash,
            Status = CredentialTokenStatus.Active,
            ExpiresAt = expiresAt
        };

        token.SetAuditOnCreate(userId, createdAt);
        token.AddDomainEvent(new EmailVerificationRequestedEvent(userId, email, createdAt));
        return token;
    }

    public void Consume(DateTimeOffset consumedAt)
    {
        if (Status != CredentialTokenStatus.Active || consumedAt > ExpiresAt)
        {
            Status = CredentialTokenStatus.Expired;
            throw new DomainException("Email verification token is already consumed or expired.");
        }

        Status = CredentialTokenStatus.Consumed;
        ConsumedAt = consumedAt;
        SetAuditOnUpdate(UserId, consumedAt);
        AddDomainEvent(new EmailVerificationCompletedEvent(UserId, consumedAt));
    }

    public void Expire()
    {
        if (Status != CredentialTokenStatus.Active) return;
        Status = CredentialTokenStatus.Expired;
    }
}
