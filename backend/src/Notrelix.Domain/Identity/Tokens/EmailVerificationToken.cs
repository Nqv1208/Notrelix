using Notrelix.Domain.Identity.Tokens.Events;

namespace Notrelix.Domain.Identity.Tokens;

public class EmailVerificationToken : AggregateRoot
{
    public Guid UserId { get; private set; }
    public TokenHash TokenHash { get; private set; } = null!;
    public UserTokenStatus Status { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }
    public DateTimeOffset? ExpiredAt { get; private set; }

    private EmailVerificationToken() : base() { }

    public static EmailVerificationToken Create(
        Guid userId,
        TokenHash tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt)
    {
        Guard.NotEmpty(userId);
        Guard.NotNull(tokenHash);

        if (expiresAt <= createdAt)
        {
            throw new BusinessRuleException("Token expiration time must be after creation time.");
        }

        var token = new EmailVerificationToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            Status = UserTokenStatus.Active,
            ExpiresAt = expiresAt
        };

        token.SetAuditOnCreate(userId, createdAt);
        token.AddDomainEvent(new EmailVerificationTokenCreatedDomainEvent(token.Id, userId, createdAt));

        return token;
    }

    public void MarkUsed(DateTimeOffset usedAt)
    {
        EnsureNotDeleted();

        if (Status == UserTokenStatus.Used)
        {
            throw new BusinessRuleException("Token has already been used.");
        }

        if (Status == UserTokenStatus.Expired || usedAt >= ExpiresAt)
        {
            throw new BusinessRuleException("Cannot use an expired token.");
        }

        Status = UserTokenStatus.Used;
        UsedAt = usedAt;
        SetAuditOnUpdate(UserId, usedAt);
        AddDomainEvent(new EmailVerificationTokenUsedDomainEvent(Id, UserId, usedAt));
    }

    public void Expire(DateTimeOffset expiredAt)
    {
        EnsureNotDeleted();

        if (Status == UserTokenStatus.Expired) return;

        if (Status == UserTokenStatus.Used)
        {
            throw new BusinessRuleException("Cannot expire a used token.");
        }

        Status = UserTokenStatus.Expired;
        ExpiredAt = expiredAt;
        SetAuditOnUpdate(UserId, expiredAt);
        AddDomainEvent(new EmailVerificationTokenExpiredDomainEvent(Id, UserId, expiredAt));
    }
}
