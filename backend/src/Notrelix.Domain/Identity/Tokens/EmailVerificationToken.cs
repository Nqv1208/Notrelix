using Notrelix.Domain.Identity.Tokens.Events;

namespace Notrelix.Domain.Identity.Tokens;

public class EmailVerificationToken : OneTimeUseToken
{
    private EmailVerificationToken() : base() { }

    public static EmailVerificationToken Create(
        Guid userId,
        TokenHash tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt)
    {
        var token = new EmailVerificationToken();
        token.Initialize(userId, tokenHash, expiresAt, createdAt);
        token.SetAuditOnCreate(userId, createdAt);
        token.AddDomainEvent(new EmailVerificationTokenCreatedDomainEvent(token.Id, userId, createdAt));
        return token;
    }

    public void MarkUsed(DateTimeOffset usedAt)
    {
        base.MarkUsed(usedAt, new EmailVerificationTokenUsedDomainEvent(Id, UserId, usedAt));
    }

    public void Expire(DateTimeOffset expiredAt)
    {
        base.TryExpire(expiredAt, new EmailVerificationTokenExpiredDomainEvent(Id, UserId, expiredAt));
    }
}
