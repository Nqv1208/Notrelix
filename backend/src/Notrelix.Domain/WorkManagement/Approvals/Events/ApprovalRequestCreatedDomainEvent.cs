namespace Notrelix.Domain.WorkManagement.Approvals.Events;

[EventName("work-management.approval-request-created")]
public sealed record ApprovalRequestCreatedDomainEvent(
    Guid AccountId,
    Guid RequestId,
    Guid WorkspaceId,
    ResourceRef Target,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
