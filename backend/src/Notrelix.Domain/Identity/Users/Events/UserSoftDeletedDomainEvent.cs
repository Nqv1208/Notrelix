namespace Notrelix.Domain.Identity.Users.Events;

public sealed record UserSoftDeletedDomainEvent(
    Guid UserId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt,
    string? Reason
) : DomainEvent(OccurredAt, workspaceId: null, DeletedBy);
