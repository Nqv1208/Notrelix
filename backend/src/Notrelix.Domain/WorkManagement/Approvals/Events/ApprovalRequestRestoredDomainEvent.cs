namespace Notrelix.Domain.WorkManagement.Approvals.Events;

public sealed record ApprovalRequestRestoredDomainEvent(
    Guid WorkspaceId,
    Guid RequestId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RestoredBy);
