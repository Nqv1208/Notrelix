namespace Notrelix.Domain.WorkManagement.Forms.Events;

[EventName("work-management.form-soft-deleted")]
public sealed record FormSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FormId,
    Guid BoardId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
