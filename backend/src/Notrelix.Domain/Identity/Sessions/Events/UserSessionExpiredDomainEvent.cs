namespace Notrelix.Domain.Identity.Sessions.Events;

[EventName("identity.user-session-expired")]
public sealed record UserSessionExpiredDomainEvent(
    Guid SessionId,
    Guid UserId,
    DateTimeOffset ExpiredAt
) : GlobalDomainEvent(ExpiredAt);
