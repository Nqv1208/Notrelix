namespace Notrelix.Application.Events.WorkManagement;

/// <summary>A to-do was ticked (IsCompleted) or unticked; titles are the values at that moment.</summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("work-management.board-item-checklist-item-completion-changed", Version = 1)]
public sealed record BoardItemChecklistItemCompletionChangedIntegrationEvent(
    Guid EventId,
    Guid AccountIdValue,
    Guid WorkspaceIdValue,
    Guid ItemId,
    Guid ChecklistId,
    string ChecklistTitle,
    string ChecklistItemTitle,
    bool IsCompleted,
    Guid ChangedBy,
    Guid CorrelationId,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default)
    : IntegrationEvent(
        EventId,
        "work-management.board-item-checklist-item-completion-changed",
        1,
        CorrelationId,
        SourceEventId,
        AccountIdValue,
        WorkspaceIdValue,
        ChangedBy,
        CausationId,
        OccurredAt);
