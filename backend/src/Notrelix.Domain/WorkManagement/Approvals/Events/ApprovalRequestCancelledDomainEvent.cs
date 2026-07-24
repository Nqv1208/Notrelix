namespace Notrelix.Domain.WorkManagement.Approvals.Events;

[EventName("work-management.approval-request-cancelled")]
public sealed record ApprovalRequestCancelledDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RequestId,
    Guid CancelledBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
