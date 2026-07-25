namespace Notrelix.Domain.Identity.Users.Events;

[EventName("identity.user-email-confirmed")]
public sealed record UserEmailConfirmedDomainEvent(
    Guid UserId,
    string Email,
    Guid? ConfirmedBy,
    DateTimeOffset ConfirmedAt
) : GlobalDomainEvent(ConfirmedAt);