namespace Notrelix.Domain.Identity.Sessions.Events;

[EventName("identity.user-session-refresh-token-rotated")]
public sealed record UserSessionRefreshTokenRotatedDomainEvent(
    Guid SessionId,
    Guid UserId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
