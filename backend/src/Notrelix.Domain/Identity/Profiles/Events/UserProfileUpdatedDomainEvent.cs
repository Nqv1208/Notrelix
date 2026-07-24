namespace Notrelix.Domain.Identity.Profiles.Events;

[EventName("identity.user-profile-updated")]
public sealed record UserProfileUpdatedDomainEvent(
    Guid UserId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
