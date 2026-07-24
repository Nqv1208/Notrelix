namespace Notrelix.Domain.Identity.Users.Events;

[EventName("identity.user-restored")]
public sealed record UserRestoredDomainEvent(
    Guid UserId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
