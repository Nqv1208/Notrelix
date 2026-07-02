namespace Notrelix.Domain.WorkManagement.Approvals.Events;

public sealed record ApprovalRequestRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RequestId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, RestoredBy);
