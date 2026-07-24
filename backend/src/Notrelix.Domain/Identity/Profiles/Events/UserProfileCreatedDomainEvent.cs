namespace Notrelix.Domain.Identity.Profiles.Events;

[EventName("identity.user-profile-created")]
public sealed record UserProfileCreatedDomainEvent(
    Guid UserProfileId,
    Guid UserId,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
