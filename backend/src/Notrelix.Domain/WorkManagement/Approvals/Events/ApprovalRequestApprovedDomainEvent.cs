namespace Notrelix.Domain.WorkManagement.Approvals.Events;

public sealed record ApprovalRequestApprovedDomainEvent(
    Guid WorkspaceId,
    Guid RequestId,
    Guid DecidedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DecidedBy);
