namespace Notrelix.Domain.WorkManagement.Forms.Events;

[EventName("work-management.form-restored")]
public sealed record FormRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid FormId,
    Guid BoardId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
