namespace Notrelix.Domain.WorkManagement.Approvals.Events;

public sealed record ApprovalRequestApprovedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RequestId,
    Guid DecidedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, DecidedBy);
