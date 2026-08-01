namespace Notrelix.Domain.Identity.Profiles.Events;

[EventName("identity.user-profile-deleted")]
public sealed record UserProfileDeletedDomainEvent(
    Guid UserProfileId,
    Guid UserId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt,
    string? Reason
) : GlobalDomainEvent(OccurredAt);
