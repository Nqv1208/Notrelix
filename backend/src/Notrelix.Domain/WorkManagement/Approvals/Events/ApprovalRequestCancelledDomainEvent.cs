namespace Notrelix.Domain.WorkManagement.Approvals.Events;

public sealed record ApprovalRequestCancelledDomainEvent(
    Guid WorkspaceId,
    Guid RequestId,
    Guid CancelledBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, CancelledBy);
