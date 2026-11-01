namespace Notrelix.Application.Events.WorkManagement;

/// <summary>A label was put on an Item; LabelName is the name at that moment, so history survives the label being renamed or deleted.</summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("work-management.board-item-label-added", Version = 1)]
public sealed record BoardItemLabelAddedIntegrationEvent(
    Guid EventId,
    Guid AccountIdValue,
    Guid WorkspaceIdValue,
    Guid ItemId,
    Guid LabelId,
    string LabelName,
    Guid AddedBy,
    Guid CorrelationId,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default)
    : IntegrationEvent(
        EventId,
        "work-management.board-item-label-added",
        1,
        CorrelationId,
        SourceEventId,
        AccountIdValue,
        WorkspaceIdValue,
        AddedBy,
        CausationId,
        OccurredAt);
