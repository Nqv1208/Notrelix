namespace Notrelix.Domain.WorkManagement.Approvals.Events;

public sealed record ApprovalRequestCreatedDomainEvent(
    Guid AccountId,
    Guid RequestId,
    Guid WorkspaceId,
    ResourceRef Target,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
