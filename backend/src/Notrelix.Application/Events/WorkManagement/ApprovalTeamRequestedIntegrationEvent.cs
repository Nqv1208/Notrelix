namespace Notrelix.Application.Events.WorkManagement;

/// <summary>A Team was named as approver of an approval request; its current members are asked to decide it.</summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("work-management.approval-team-requested", Version = 1)]
public sealed record ApprovalTeamRequestedIntegrationEvent(
    Guid EventId,
    Guid AccountIdValue,
    Guid WorkspaceIdValue,
    Guid RequestId,
    string TargetKind,
    Guid TargetId,
    Guid ApproverTeamId,
    Guid RequestedBy,
    string Title,
    string? Note,
    Guid CorrelationId,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default)
    : IntegrationEvent(
        EventId,
        "work-management.approval-team-requested",
        1,
        CorrelationId,
        SourceEventId,
        AccountIdValue,
        WorkspaceIdValue,
        RequestedBy,
        CausationId,
        OccurredAt);
