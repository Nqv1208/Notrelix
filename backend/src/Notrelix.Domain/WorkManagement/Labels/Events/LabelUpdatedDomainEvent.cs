namespace Notrelix.Domain.WorkManagement.Labels.Events;

[EventName("work-management.label-updated")]
public sealed record LabelUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid LabelId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
