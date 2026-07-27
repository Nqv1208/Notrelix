namespace Notrelix.Domain.Identity.Sessions.Events;

[EventName("identity.user-session-revoked")]
public sealed record UserSessionRevokedDomainEvent(
    Guid SessionId,
    Guid UserId,
    DateTimeOffset RevokedAt,
    string? Reason
) : GlobalDomainEvent(RevokedAt);
