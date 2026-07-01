namespace Notrelix.Domain.WorkManagement.Approvals.Events;

public sealed record ApprovalRequestRejectedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RequestId,
    Guid DecidedBy,
    string? Note,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, DecidedBy);
