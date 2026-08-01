namespace Notrelix.Domain.Identity.Users.Events;

[EventName("identity.user-restored")]
public sealed record UserRestoredDomainEvent(
    Guid UserId,
    UserStatus Status,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
