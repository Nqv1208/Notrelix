using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Notrelix.Domain.Identity.Sessions.Events;

namespace Notrelix.Domain.Identity.Sessions;

public class UserSession : AggregateRoot
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
            throw new BusinessRuleException("Session expiration time must be after creation time.");
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
        session.AddDomainEvent(new UserSessionCreatedEvent(session.Id, userId, createdAt));
        
        return session;
    }

    public void UpdateRefreshToken(RefreshTokenHash newTokenHash, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        Guard.NotNull(newTokenHash);

        if (Status != SessionStatus.Active)
        {
            throw new BusinessRuleException("Cannot update refresh token for an inactive session.");
        }

        RefreshTokenHash = newTokenHash;
        SetAuditOnUpdate(UserId, updatedAt);
    }

    public void Revoke(DateTimeOffset revokedAt, string? reason = null)
    {
        EnsureNotDeleted();
        if (Status == SessionStatus.Revoked) return;

        if (Status == SessionStatus.Expired)
        {
            throw new BusinessRuleException("Cannot revoke an expired session.");
        }

        Status = SessionStatus.Revoked;
        RevokedAt = revokedAt;

        SetAuditOnUpdate(UserId, revokedAt);
        AddDomainEvent(new UserSessionRevokedEvent(Id, UserId, revokedAt, reason));
    }

    public void Expire(DateTimeOffset expiredAt)
    {
        EnsureNotDeleted();
        if (Status == SessionStatus.Expired) return;

        if (Status == SessionStatus.Revoked)
        {
            throw new BusinessRuleException("Cannot expire a revoked session.");
        }

        Status = SessionStatus.Expired;
        ExpiredAt = expiredAt;

        SetAuditOnUpdate(UserId, expiredAt);
        AddDomainEvent(new UserSessionExpiredEvent(Id, UserId, expiredAt));
    }
}
