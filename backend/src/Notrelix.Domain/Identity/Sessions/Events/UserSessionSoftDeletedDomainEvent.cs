namespace Notrelix.Domain.Identity.Sessions.Events;

[EventName("identity.user-session-soft-deleted")]
public sealed record UserSessionSoftDeletedDomainEvent(
    Guid SessionId,
    Guid UserId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt,
    string? Reason
) : GlobalDomainEvent(OccurredAt);
