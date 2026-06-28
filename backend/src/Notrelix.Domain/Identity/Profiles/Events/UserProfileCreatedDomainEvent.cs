namespace Notrelix.Domain.Identity.Profiles.Events;

public sealed record UserProfileCreatedDomainEvent(
    Guid UserProfileId,
    Guid UserId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt, UserId);
