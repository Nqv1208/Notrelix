namespace Notrelix.Domain.WorkManagement.Labels.Events;

[EventName("work-management.label-deleted")]
public sealed record LabelDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid LabelId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
