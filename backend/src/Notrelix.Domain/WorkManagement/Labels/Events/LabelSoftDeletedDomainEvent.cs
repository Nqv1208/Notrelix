namespace Notrelix.Domain.WorkManagement.Labels.Events;

public sealed record LabelSoftDeletedDomainEvent(
    Guid WorkspaceId,
    Guid LabelId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, DeletedBy);
