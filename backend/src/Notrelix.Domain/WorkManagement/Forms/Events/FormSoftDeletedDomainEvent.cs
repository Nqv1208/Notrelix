namespace Notrelix.Domain.WorkManagement.Forms.Events;

public sealed record FormSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FormId,
    Guid BoardId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, DeletedBy);
