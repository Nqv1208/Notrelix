namespace Notrelix.Application.Events.WorkManagement;

/// <summary>A checklist on an Item was renamed; ChecklistTitle is the new title.</summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("work-management.board-item-checklist-renamed", Version = 1)]
public sealed record BoardItemChecklistRenamedIntegrationEvent(
    Guid EventId,
    Guid AccountIdValue,
    Guid WorkspaceIdValue,
    Guid ItemId,
    Guid ChecklistId,
    string ChecklistTitle,
    Guid RenamedBy,
    Guid CorrelationId,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default)
    : IntegrationEvent(
        EventId,
        "work-management.board-item-checklist-renamed",
        1,
        CorrelationId,
        SourceEventId,
        AccountIdValue,
        WorkspaceIdValue,
        RenamedBy,
        CausationId,
        OccurredAt);
