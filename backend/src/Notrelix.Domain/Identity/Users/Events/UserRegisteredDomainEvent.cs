namespace Notrelix.Domain.Identity.Users.Events;

public sealed record UserRegisteredDomainEvent(
    Guid UserId,
    Email Email,
    DateTimeOffset RegisteredAt
) : GlobalDomainEvent(RegisteredAt);
