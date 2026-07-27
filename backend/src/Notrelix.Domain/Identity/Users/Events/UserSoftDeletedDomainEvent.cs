namespace Notrelix.Domain.Identity.Users.Events;

[EventName("identity.user-soft-deleted")]
public sealed record UserSoftDeletedDomainEvent(
    Guid UserId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt,
    string? Reason
) : GlobalDomainEvent(OccurredAt);
