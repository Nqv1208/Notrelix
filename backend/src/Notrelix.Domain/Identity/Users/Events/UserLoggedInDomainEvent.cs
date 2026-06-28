namespace Notrelix.Domain.Identity.Users.Events;

public sealed record UserLoggedInDomainEvent(
    Guid UserId,
    DateTimeOffset LoggedInAt
) : GlobalDomainEvent(LoggedInAt);
