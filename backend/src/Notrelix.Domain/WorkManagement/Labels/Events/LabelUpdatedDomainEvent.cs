namespace Notrelix.Domain.WorkManagement.Labels.Events;

public sealed record LabelUpdatedDomainEvent(
    Guid WorkspaceId,
    Guid LabelId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
