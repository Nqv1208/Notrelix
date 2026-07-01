namespace Notrelix.Domain.WorkManagement.Approvals.Events;

public sealed record ApprovalRequestCancelledDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RequestId,
    Guid CancelledBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, CancelledBy);
