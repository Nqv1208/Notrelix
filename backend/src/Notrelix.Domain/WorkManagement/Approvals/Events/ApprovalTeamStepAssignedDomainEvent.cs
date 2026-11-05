namespace Notrelix.Domain.WorkManagement.Approvals.Events;

/// <summary>A Team was named as an approver of a request: its current members are asked to decide it.</summary>
[EventName("work-management.approval-team-step-assigned")]
public sealed record ApprovalTeamStepAssignedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RequestId,
    Guid StepId,
    Guid ApproverTeamId,
    Guid RequestedBy,
    ResourceRef Target,
    string Title,
    string? Note,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
