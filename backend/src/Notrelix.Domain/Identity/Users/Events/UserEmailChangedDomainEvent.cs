namespace Notrelix.Domain.Identity.Users.Events;

public sealed record UserEmailChangedDomainEvent(
    Guid UserId,
    Email OldEmail,
    Email NewEmail,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, null, null);
