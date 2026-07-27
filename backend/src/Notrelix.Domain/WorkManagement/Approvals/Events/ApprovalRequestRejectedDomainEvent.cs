namespace Notrelix.Domain.WorkManagement.Approvals.Events;

[EventName("work-management.approval-request-rejected")]
public sealed record ApprovalRequestRejectedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RequestId,
    Guid DecidedBy,
    string? Note,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
