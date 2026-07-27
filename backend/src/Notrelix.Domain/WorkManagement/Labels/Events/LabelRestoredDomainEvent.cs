namespace Notrelix.Domain.WorkManagement.Labels.Events;

[EventName("work-management.label-restored")]
public sealed record LabelRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid LabelId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
