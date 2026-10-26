namespace Notrelix.Application.Events.WorkManagement;

/// <summary>An approval request was approved or rejected by an approver.</summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("work-management.approval-decided", Version = 1)]
public sealed record ApprovalDecidedIntegrationEvent(
    Guid EventId,
    Guid AccountIdValue,
    Guid WorkspaceIdValue,
    Guid RequestId,
    string TargetKind,
    Guid TargetId,
    Guid RequestedBy,
    Guid DecidedBy,
    bool Approved,
    string? Note,
    Guid CorrelationId,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default)
    : IntegrationEvent(
        EventId,
        "work-management.approval-decided",
        1,
        CorrelationId,
        SourceEventId,
        AccountIdValue,
        WorkspaceIdValue,
        DecidedBy,
        CausationId,
        OccurredAt);
