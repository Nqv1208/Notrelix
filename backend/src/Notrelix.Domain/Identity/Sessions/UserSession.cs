using Notrelix.Domain.Identity.Sessions.Events;

namespace Notrelix.Domain.Identity.Sessions;

public sealed class UserSession : AggregateRoot
{
    public Guid UserId { get; private set; }
    public RefreshTokenHash RefreshTokenHash { get; private set; } = null!;
    /// <summary>The credential the last rotation replaced; only its digest, only for the grace window and replay detection.</summary>
    public string? PreviousRefreshTokenHash { get; private set; }
    public DateTimeOffset? RotatedAt { get; private set; }
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

    /// <summary>
    /// Swaps in the next refresh credential and keeps the one it replaces for the rotation grace window,
    /// so a lost response can be retried and a later replay of the old credential can be told from a
    /// retry. The session lives on idle for <see cref="SessionLifetimePolicy.IdleLifetime"/> after this
    /// call, never past <see cref="SessionLifetimePolicy.AbsoluteLifetime"/> after it was created.
    /// </summary>
    public void Rotate(RefreshTokenHash newTokenHash, DateTimeOffset now)
    {
        Guard.NotNull(newTokenHash);

        if (Status != SessionStatus.Active)
        {
            throw new BusinessRuleException(IdentityRuleCodes.Identity_Session_CannotUpdateRefreshTokenOfInactive, "Cannot update refresh token for an inactive session.");
        }

        var expiresAt = SessionLifetimePolicy.ExpiryAfterRefresh(CreatedAt, now);
        if (expiresAt <= now)
        {
            throw new BusinessRuleException(IdentityRuleCodes.Identity_Session_ExpirationMustBeAfterCreation, "Session expiration time must be after creation time.");
        }

        var pending = PrepareAuditUpdate(UserId, now);
        PreviousRefreshTokenHash = RefreshTokenHash.Hash;
        RefreshTokenHash = newTokenHash;
        RotatedAt = now;
        ExpiresAt = expiresAt;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new UserSessionRefreshTokenRotatedDomainEvent(Id, UserId, now));
    }

    /// <summary>
    /// True when <paramref name="presentedHash"/> is the credential this session just replaced and the
    /// grace window has not closed: a retry, not a replay.
    /// </summary>
    public bool IsWithinRotationGrace(string presentedHash, DateTimeOffset now) =>
        PreviousRefreshTokenHash == presentedHash
        && RotatedAt is { } rotatedAt
        && now - rotatedAt <= SessionLifetimePolicy.RotationGrace;

    public void Revoke(DateTimeOffset revokedAt, string? reason = null)
    {
        if (Status == SessionStatus.Revoked) return;

        if (Status == SessionStatus.Expired)
        {
            throw new BusinessRuleException(IdentityRuleCodes.Identity_Session_CannotRevokeExpired, "Cannot revoke an expired session.");
        }

        var pending = PrepareAuditUpdate(UserId, revokedAt);
        Status = SessionStatus.Revoked;
        RevokedAt = revokedAt;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new UserSessionRevokedDomainEvent(Id, UserId, revokedAt, reason));
    }

    public void Expire(DateTimeOffset expiredAt)
    {
        if (Status == SessionStatus.Expired) return;

        if (Status == SessionStatus.Revoked)
        {
            throw new BusinessRuleException(IdentityRuleCodes.Identity_Session_CannotExpireRevoked, "Cannot expire a revoked session.");
        }

        var pending = PrepareAuditUpdate(UserId, expiredAt);
        Status = SessionStatus.Expired;
        ExpiredAt = expiredAt;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new UserSessionExpiredDomainEvent(Id, UserId, expiredAt));
    }
}
