namespace Notrelix.Domain.Documents.Pages.Events;

public sealed record PageSoftDeletedDomainEvent(
    Guid WorkspaceId,
    Guid PageId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, DeletedBy);
