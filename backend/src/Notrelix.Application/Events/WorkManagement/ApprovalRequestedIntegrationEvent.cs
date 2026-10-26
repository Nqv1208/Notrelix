namespace Notrelix.Application.Events.WorkManagement;

/// <summary>A person was named as approver of an approval request and is asked to decide it.</summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("work-management.approval-requested", Version = 1)]
public sealed record ApprovalRequestedIntegrationEvent(
    Guid EventId,
    Guid AccountIdValue,
    Guid WorkspaceIdValue,
    Guid RequestId,
    string TargetKind,
    Guid TargetId,
    Guid ApproverUserId,
    Guid RequestedBy,
    string Title,
    string? Note,
    Guid CorrelationId,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default)
    : IntegrationEvent(
        EventId,
        "work-management.approval-requested",
        1,
        CorrelationId,
        SourceEventId,
        AccountIdValue,
        WorkspaceIdValue,
        RequestedBy,
        CausationId,
        OccurredAt);
