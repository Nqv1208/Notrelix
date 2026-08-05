namespace Notrelix.Domain.WorkManagement.Forms.Events;

[EventName("work-management.form-deleted")]
public sealed record FormDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FormId,
    Guid BoardId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
