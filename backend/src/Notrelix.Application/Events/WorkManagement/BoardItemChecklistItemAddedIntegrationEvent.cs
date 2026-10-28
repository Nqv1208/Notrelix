namespace Notrelix.Application.Events.WorkManagement;

/// <summary>A to-do was added to a checklist; titles are the values at that moment.</summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("work-management.board-item-checklist-item-added", Version = 1)]
public sealed record BoardItemChecklistItemAddedIntegrationEvent(
    Guid EventId,
    Guid AccountIdValue,
    Guid WorkspaceIdValue,
    Guid ItemId,
    Guid ChecklistId,
    string ChecklistTitle,
    string ChecklistItemTitle,
    Guid AddedBy,
    Guid CorrelationId,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default)
    : IntegrationEvent(
        EventId,
        "work-management.board-item-checklist-item-added",
        1,
        CorrelationId,
        SourceEventId,
        AccountIdValue,
        WorkspaceIdValue,
        AddedBy,
        CausationId,
        OccurredAt);
