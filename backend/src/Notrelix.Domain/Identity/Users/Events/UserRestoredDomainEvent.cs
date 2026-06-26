namespace Notrelix.Domain.Identity.Users.Events;

public sealed record UserRestoredDomainEvent(
    Guid UserId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, workspaceId: null, RestoredBy);
