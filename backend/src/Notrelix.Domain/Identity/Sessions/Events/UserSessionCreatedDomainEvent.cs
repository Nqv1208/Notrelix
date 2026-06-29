namespace Notrelix.Domain.Identity.Sessions.Events;

public sealed record UserSessionCreatedDomainEvent(
    Guid SessionId,
    Guid UserId,
    DateTimeOffset CreatedAt
) : GlobalDomainEvent(CreatedAt);
