namespace Notrelix.Domain.WorkManagement.Forms.Events;

public sealed record FormRestoredDomainEvent(
    Guid WorkspaceId,
    Guid FormId,
    Guid BoardId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RestoredBy);
