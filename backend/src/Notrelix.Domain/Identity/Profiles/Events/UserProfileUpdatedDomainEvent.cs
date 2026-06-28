namespace Notrelix.Domain.Identity.Profiles.Events;

public sealed record UserProfileUpdatedDomainEvent(
    Guid UserId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
