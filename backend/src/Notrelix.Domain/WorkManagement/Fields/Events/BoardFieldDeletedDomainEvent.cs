namespace Notrelix.Domain.WorkManagement.Fields.Events;

public sealed record BoardFieldDeletedDomainEvent(
    Guid WorkspaceId,
    Guid FieldId,
    Guid BoardId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, DeletedBy);
