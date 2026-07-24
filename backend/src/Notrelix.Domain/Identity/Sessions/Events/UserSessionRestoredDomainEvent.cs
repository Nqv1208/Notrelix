namespace Notrelix.Domain.Identity.Sessions.Events;

[EventName("identity.user-session-restored")]
public sealed record UserSessionRestoredDomainEvent(
    Guid SessionId,
    Guid UserId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : GlobalDomainEvent(OccurredAt);
