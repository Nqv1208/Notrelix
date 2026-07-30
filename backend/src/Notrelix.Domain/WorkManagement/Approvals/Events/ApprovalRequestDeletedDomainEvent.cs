namespace Notrelix.Domain.WorkManagement.Approvals.Events;

[EventName("work-management.approval-request-deleted")]
public sealed record ApprovalRequestDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RequestId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
