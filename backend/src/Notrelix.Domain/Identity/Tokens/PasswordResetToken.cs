using Notrelix.Domain.Identity.Tokens.Events;

namespace Notrelix.Domain.Identity.Tokens;

public sealed class PasswordResetToken : OneTimeUseToken
{
    private PasswordResetToken() : base() { }

    public static PasswordResetToken Create(
        Guid userId,
        TokenHash tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt)
    {
        var token = new PasswordResetToken();
        token.Initialize(userId, tokenHash, 1, expiresAt, createdAt);
        token.SetAuditOnCreate(userId, createdAt);
        token.RaiseDomainEvent(new PasswordResetTokenCreatedDomainEvent(token.Id, userId, createdAt));
        return token;
    }

    public void MarkUsed(DateTimeOffset usedAt)
    {
        base.MarkUsed(usedAt, new PasswordResetTokenUsedDomainEvent(Id, UserId, usedAt));
    }

    public void Expire(DateTimeOffset expiredAt)
    {
        base.TryExpire(expiredAt, new PasswordResetTokenExpiredDomainEvent(Id, UserId, expiredAt));
    }

    public bool Revoke(DateTimeOffset revokedAt, string revocationReason)
    {
        return base.TryRevoke(revokedAt, revocationReason, new PasswordResetTokenRevokedDomainEvent(Id, UserId, revokedAt));
    }
}
