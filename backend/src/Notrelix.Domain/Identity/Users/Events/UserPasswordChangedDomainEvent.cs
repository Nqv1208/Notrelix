namespace Notrelix.Domain.Identity.Users.Events;

public sealed record UserPasswordChangedDomainEvent(
    Guid UserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, null, null);
