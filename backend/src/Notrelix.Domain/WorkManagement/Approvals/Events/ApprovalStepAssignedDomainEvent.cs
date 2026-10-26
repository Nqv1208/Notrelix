namespace Notrelix.Domain.WorkManagement.Approvals.Events;

/// <summary>A person was named as an approver of a request: they are asked to decide it.</summary>
[EventName("work-management.approval-step-assigned")]
public sealed record ApprovalStepAssignedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RequestId,
    Guid StepId,
    Guid ApproverUserId,
    Guid RequestedBy,
    ResourceRef Target,
    string Title,
    string? Note,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
