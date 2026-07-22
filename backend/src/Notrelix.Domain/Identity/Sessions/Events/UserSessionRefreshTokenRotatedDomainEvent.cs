namespace Notrelix.Domain.Identity.Sessions.Events;

public sealed record UserSessionRefreshTokenRotatedDomainEvent(
    Guid SessionId,
    Guid UserId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
