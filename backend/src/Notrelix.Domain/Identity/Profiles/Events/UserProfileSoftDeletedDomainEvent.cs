namespace Notrelix.Domain.Identity.Profiles.Events;

[EventName("identity.user-profile-soft-deleted")]
public sealed record UserProfileSoftDeletedDomainEvent(
    Guid UserProfileId,
    Guid UserId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt,
    string? Reason
) : GlobalDomainEvent(OccurredAt);
