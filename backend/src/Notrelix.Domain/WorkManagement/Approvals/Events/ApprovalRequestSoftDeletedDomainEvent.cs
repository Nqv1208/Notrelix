namespace Notrelix.Domain.WorkManagement.Approvals.Events;

public sealed record ApprovalRequestSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RequestId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, DeletedBy);
