namespace Notrelix.Domain.Identity.Users.Events;

[EventName("identity.user-email-changed")]
public sealed record UserEmailChangedDomainEvent(
    Guid UserId,
    Email OldEmail,
    Email NewEmail,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
