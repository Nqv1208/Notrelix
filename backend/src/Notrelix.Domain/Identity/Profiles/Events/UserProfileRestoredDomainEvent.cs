namespace Notrelix.Domain.Identity.Profiles.Events;

[EventName("identity.user-profile-restored")]
public sealed record UserProfileRestoredDomainEvent(
    Guid UserProfileId,
    Guid UserId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
