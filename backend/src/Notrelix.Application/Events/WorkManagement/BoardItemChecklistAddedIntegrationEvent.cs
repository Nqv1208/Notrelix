namespace Notrelix.Application.Events.WorkManagement;

/// <summary>A checklist was added to an Item.</summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("work-management.board-item-checklist-added", Version = 1)]
public sealed record BoardItemChecklistAddedIntegrationEvent(
    Guid EventId,
    Guid AccountIdValue,
    Guid WorkspaceIdValue,
    Guid ItemId,
    Guid ChecklistId,
    string ChecklistTitle,
    Guid AddedBy,
    Guid CorrelationId,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default)
    : IntegrationEvent(
        EventId,
        "work-management.board-item-checklist-added",
        1,
        CorrelationId,
        SourceEventId,
        AccountIdValue,
        WorkspaceIdValue,
        AddedBy,
        CausationId,
        OccurredAt);
