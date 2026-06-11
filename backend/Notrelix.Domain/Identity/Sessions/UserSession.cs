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

    public static UserSession Create(Guid userId, RefreshTokenHash tokenHash, DateTimeOffset expiresAt, string? ipAddress = null, string? userAgent = null)
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
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        session.AddDomainEvent(new UserSessionCreatedEvent(session.Id, userId));
        return session;
    }

    public void Revoke()
    {
        if (Status != SessionStatus.Active) return;
        Status = SessionStatus.Revoked;
        AddDomainEvent(new UserSessionRevokedEvent(Id, UserId));
    }
}
