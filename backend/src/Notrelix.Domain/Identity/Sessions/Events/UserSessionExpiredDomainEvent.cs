namespace Notrelix.Domain.Identity.Sessions.Events;

public sealed record UserSessionExpiredDomainEvent(
    Guid SessionId,
    Guid UserId,
    DateTimeOffset ExpiredAt
) : GlobalDomainEvent(ExpiredAt);
