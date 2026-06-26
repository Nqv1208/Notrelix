namespace Notrelix.Domain.Identity.Sessions.Events;

public sealed record UserSessionSoftDeletedDomainEvent(
    Guid SessionId,
    Guid UserId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt,
    string? Reason
) : DomainEvent(OccurredAt, workspaceId: null, DeletedBy);
