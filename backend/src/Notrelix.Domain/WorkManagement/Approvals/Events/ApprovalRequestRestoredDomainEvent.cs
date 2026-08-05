namespace Notrelix.Domain.WorkManagement.Approvals.Events;

[EventName("work-management.approval-request-restored")]
public sealed record ApprovalRequestRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RequestId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
