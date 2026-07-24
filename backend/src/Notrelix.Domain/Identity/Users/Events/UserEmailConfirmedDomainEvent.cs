namespace Notrelix.Domain.Identity.Users.Events;

[EventName("identity.user-email-confirmed")]
public sealed record UserEmailConfirmedDomainEvent(
    Guid UserId,
    string Email,
    DateTimeOffset ConfirmedAt
) : GlobalDomainEvent(ConfirmedAt);
