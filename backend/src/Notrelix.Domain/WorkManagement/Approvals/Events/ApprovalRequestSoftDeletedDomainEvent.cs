namespace Notrelix.Domain.WorkManagement.Approvals.Events;

public sealed record ApprovalRequestSoftDeletedDomainEvent(
    Guid WorkspaceId,
    Guid RequestId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DeletedBy);
