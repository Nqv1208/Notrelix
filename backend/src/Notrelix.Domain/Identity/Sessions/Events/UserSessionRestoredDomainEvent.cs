namespace Notrelix.Domain.Identity.Sessions.Events;

public sealed record UserSessionRestoredDomainEvent(
    Guid SessionId,
    Guid UserId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, workspaceId: null, RestoredBy);
