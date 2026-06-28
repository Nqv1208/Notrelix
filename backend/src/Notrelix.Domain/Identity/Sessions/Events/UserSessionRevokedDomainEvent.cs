namespace Notrelix.Domain.Identity.Sessions.Events;

public sealed record UserSessionRevokedDomainEvent(
    Guid SessionId,
    Guid UserId,
    DateTimeOffset RevokedAt,
    string? Reason
) : GlobalDomainEvent(RevokedAt);
