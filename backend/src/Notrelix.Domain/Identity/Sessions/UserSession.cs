using Notrelix.Domain.Identity.Sessions.Events;

namespace Notrelix.Domain.Identity.Sessions;

public class UserSession : SoftDeletableAggregateRoot
{
    public Guid UserId { get; private set; }
    public RefreshTokenHash RefreshTokenHash { get; private set; } = null!;
    public SessionStatus Status { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public DateTimeOffset? ExpiredAt { get; private set; }

    private UserSession() : base() { }

    public static UserSession Create(
        Guid userId,
        RefreshTokenHash tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt,
        string? ipAddress = null,
        string? userAgent = null)
    {
        Guard.NotEmpty(userId);
        Guard.NotNull(tokenHash);

        if (expiresAt <= createdAt)
        {
            throw new BusinessRuleException(IdentityRuleCodes.Identity_Session_ExpirationMustBeAfterCreation, "Session expiration time must be after creation time.");
        }

        var session = new UserSession
        {
            UserId = userId,
            RefreshTokenHash = tokenHash,
            Status = SessionStatus.Active,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ExpiresAt = expiresAt
        };

        session.SetAuditOnCreate(userId, createdAt);
        session.RaiseDomainEvent(new UserSessionCreatedDomainEvent(session.Id, userId, createdAt));

        return session;
    }

    public void UpdateRefreshToken(RefreshTokenHash newTokenHash, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(newTokenHash);

        if (Status != SessionStatus.Active)
        {
            throw new BusinessRuleException(IdentityRuleCodes.Identity_Session_CannotUpdateRefreshTokenOfInactive, "Cannot update refresh token for an inactive session.");
        }

        RefreshTokenHash = newTokenHash;
        SetAuditOnUpdate(UserId, updatedAt);
        IncrementVersion();
        RaiseDomainEvent(new UserSessionRefreshTokenRotatedDomainEvent(Id, UserId, updatedAt));
    }

    public void Revoke(DateTimeOffset revokedAt, string? reason = null)
    {
        EnsureNotDeleted();
        if (Status == SessionStatus.Revoked) return;

        if (Status == SessionStatus.Expired)
        {
            throw new BusinessRuleException(IdentityRuleCodes.Identity_Session_CannotRevokeExpired, "Cannot revoke an expired session.");
        }

        Status = SessionStatus.Revoked;
        RevokedAt = revokedAt;

        SetAuditOnUpdate(UserId, revokedAt);
        IncrementVersion();
        RaiseDomainEvent(new UserSessionRevokedDomainEvent(Id, UserId, revokedAt, reason));
    }

    public void Expire(DateTimeOffset expiredAt)
    {
        EnsureNotDeleted();
        if (Status == SessionStatus.Expired) return;

        if (Status == SessionStatus.Revoked)
        {
            throw new BusinessRuleException(IdentityRuleCodes.Identity_Session_CannotExpireRevoked, "Cannot expire a revoked session.");
        }

        Status = SessionStatus.Expired;
        ExpiredAt = expiredAt;

        SetAuditOnUpdate(UserId, expiredAt);
        IncrementVersion();
        RaiseDomainEvent(new UserSessionExpiredDomainEvent(Id, UserId, expiredAt));
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
        RaiseDomainEvent(new UserSessionSoftDeletedDomainEvent(Id, UserId, deletedBy, deletedAt, reason));
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
        RaiseDomainEvent(new UserSessionRestoredDomainEvent(Id, UserId, restoredBy, restoredAt));
    }
}
