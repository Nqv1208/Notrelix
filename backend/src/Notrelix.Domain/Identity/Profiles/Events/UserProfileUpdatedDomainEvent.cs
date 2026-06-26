namespace Notrelix.Domain.Identity.Profiles.Events;

public sealed record UserProfileUpdatedDomainEvent(
    Guid UserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, null, null);
