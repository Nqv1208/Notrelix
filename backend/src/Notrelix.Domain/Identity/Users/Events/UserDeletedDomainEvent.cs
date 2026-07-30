namespace Notrelix.Domain.Identity.Users.Events;

[EventName("identity.user-deleted")]
public sealed record UserDeletedDomainEvent(
    Guid UserId,
    UserStatus Status,
    Guid DeletedBy,
    DateTimeOffset OccurredAt,
    string? Reason
) : GlobalDomainEvent(OccurredAt);
