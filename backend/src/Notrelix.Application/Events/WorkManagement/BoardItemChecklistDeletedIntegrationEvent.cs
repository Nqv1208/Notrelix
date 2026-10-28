namespace Notrelix.Application.Events.WorkManagement;

/// <summary>A checklist was deleted from an Item; ChecklistTitle is its title at that moment.</summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("work-management.board-item-checklist-deleted", Version = 1)]
public sealed record BoardItemChecklistDeletedIntegrationEvent(
    Guid EventId,
    Guid AccountIdValue,
    Guid WorkspaceIdValue,
    Guid ItemId,
    Guid ChecklistId,
    string ChecklistTitle,
    Guid DeletedBy,
    Guid CorrelationId,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default)
    : IntegrationEvent(
        EventId,
        "work-management.board-item-checklist-deleted",
        1,
        CorrelationId,
        SourceEventId,
        AccountIdValue,
        WorkspaceIdValue,
        DeletedBy,
        CausationId,
        OccurredAt);
