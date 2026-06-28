namespace Notrelix.Domain.Identity.Users.Events;

public sealed record UserSoftDeletedDomainEvent(
    Guid UserId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt,
    string? Reason
) : GlobalDomainEvent(OccurredAt, DeletedBy);
