namespace Notrelix.Domain.Identity.Profiles.Events;

[EventName("identity.user-profile-updated")]
public sealed record UserProfileUpdatedDomainEvent(
    Guid UserId,
    Guid? UpdatedBy,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);