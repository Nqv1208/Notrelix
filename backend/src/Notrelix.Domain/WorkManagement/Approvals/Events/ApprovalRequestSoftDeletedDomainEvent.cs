namespace Notrelix.Domain.WorkManagement.Approvals.Events;

[EventName("work-management.approval-request-soft-deleted")]
public sealed record ApprovalRequestSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RequestId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
