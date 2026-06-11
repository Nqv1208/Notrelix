using Notrelix.Domain.Common;

namespace Notrelix.Domain.Identity.Sessions;

public class UserSession : AggregateRoot
{
    public Guid UserId { get; private set; }
    public RefreshTokenHash RefreshTokenHash { get; private set; } = null!;
    public SessionStatus Status { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }

    private UserSession() : base() { }

    public static UserSession Create(Guid userId, RefreshTokenHash tokenHash, DateTimeOffset expiresAt, DateTimeOffset createdAt, string? ipAddress = null, string? userAgent = null)
    {
        Guard.NotEmpty(userId);
        Guard.NotNull(tokenHash);

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

    public void Revoke(DateTimeOffset revokedAt)
    {
        if (Status != SessionStatus.Active) return;
        Status = SessionStatus.Revoked;
        SetAuditOnUpdate(UserId, revokedAt);
        AddDomainEvent(new UserSessionRevokedEvent(Id, UserId, revokedAt));
    }
}
