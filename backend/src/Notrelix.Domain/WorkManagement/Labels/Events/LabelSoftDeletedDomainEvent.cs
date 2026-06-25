namespace Notrelix.Domain.WorkManagement.Labels.Events;

public sealed record LabelSoftDeletedDomainEvent(
    Guid WorkspaceId,
    Guid LabelId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DeletedBy);
