namespace Notrelix.Domain.WorkManagement.Labels.Events;

[EventName("work-management.label-soft-deleted")]
public sealed record LabelSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid LabelId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
