namespace Notrelix.Domain.WorkManagement.Approvals.Events;

public sealed record ApprovalRequestCreatedDomainEvent(
    Guid RequestId,
    Guid WorkspaceId,
    ResourceRef Target,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
