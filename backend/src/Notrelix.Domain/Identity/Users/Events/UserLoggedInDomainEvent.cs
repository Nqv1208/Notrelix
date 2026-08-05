namespace Notrelix.Domain.Identity.Users.Events;

[EventName("identity.user-logged-in")]
public sealed record UserLoggedInDomainEvent(
    Guid UserId,
    DateTimeOffset LoggedInAt
) : GlobalDomainEvent(LoggedInAt);
