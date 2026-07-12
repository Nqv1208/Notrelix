using Notrelix.Domain.Identity.Tokens.Events;

namespace Notrelix.Domain.Identity.Tokens;

public class EmailVerificationToken : OneTimeUseToken
{
    public string? NormalizedEmailSnapshot { get; private set; }

    private EmailVerificationToken() : base() { }

    public static EmailVerificationToken Create(
        Guid userId,
        TokenHash tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt)
        => Create(userId, tokenHash, 1, null, expiresAt, createdAt);

    public static EmailVerificationToken Create(
        Guid userId,
        TokenHash tokenHash,
        int hashVersion,
        string? normalizedEmailSnapshot,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt)
    {
        var token = new EmailVerificationToken();
        token.Initialize(userId, tokenHash, hashVersion, expiresAt, createdAt);
        token.NormalizedEmailSnapshot = string.IsNullOrWhiteSpace(normalizedEmailSnapshot)
            ? null
            : SharedKernel.Email.Create(normalizedEmailSnapshot).Value;
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

    public bool Revoke(DateTimeOffset revokedAt, string revocationReason)
    {
        return base.TryRevoke(revokedAt, revocationReason, new EmailVerificationTokenRevokedDomainEvent(Id, UserId, revokedAt));
    }

}
