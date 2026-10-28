namespace Notrelix.Application.Events.WorkManagement;

/// <summary>A label was taken off an Item (or its label was deleted); LabelName is the name at that moment, so history survives the label being renamed or deleted.</summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("work-management.board-item-label-removed", Version = 1)]
public sealed record BoardItemLabelRemovedIntegrationEvent(
    Guid EventId,
    Guid AccountIdValue,
    Guid WorkspaceIdValue,
    Guid ItemId,
    Guid LabelId,
    string LabelName,
    Guid RemovedBy,
    Guid CorrelationId,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default)
    : IntegrationEvent(
        EventId,
        "work-management.board-item-label-removed",
        1,
        CorrelationId,
        SourceEventId,
        AccountIdValue,
        WorkspaceIdValue,
        RemovedBy,
        CausationId,
        OccurredAt);
