namespace Notrelix.Application.Events.WorkManagement;

/// <summary>A to-do was removed from a checklist; titles are the values at that moment.</summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("work-management.board-item-checklist-item-removed", Version = 1)]
public sealed record BoardItemChecklistItemRemovedIntegrationEvent(
    Guid EventId,
    Guid AccountIdValue,
    Guid WorkspaceIdValue,
    Guid ItemId,
    Guid ChecklistId,
    string ChecklistTitle,
    string ChecklistItemTitle,
    Guid RemovedBy,
    Guid CorrelationId,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default)
    : IntegrationEvent(
        EventId,
        "work-management.board-item-checklist-item-removed",
        1,
        CorrelationId,
        SourceEventId,
        AccountIdValue,
        WorkspaceIdValue,
        RemovedBy,
        CausationId,
        OccurredAt);
