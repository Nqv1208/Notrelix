namespace Notrelix.Application.Events.WorkManagement;

/// <summary>The start or due date of an Item changed; the previous and new values are instants, null when unset.</summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("work-management.board-item-dates-changed", Version = 1)]
public sealed record BoardItemDatesChangedIntegrationEvent(
    Guid EventId,
    Guid AccountIdValue,
    Guid WorkspaceIdValue,
    Guid ItemId,
    Guid BoardId,
    DateTimeOffset? StartedAt,
    DateTimeOffset? DueAt,
    DateTimeOffset? PreviousStartedAt,
    DateTimeOffset? PreviousDueAt,
    Guid ChangedBy,
    Guid CorrelationId,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default)
    : IntegrationEvent(
        EventId,
        "work-management.board-item-dates-changed",
        1,
        CorrelationId,
        SourceEventId,
        AccountIdValue,
        WorkspaceIdValue,
        ChangedBy,
        CausationId,
        OccurredAt);
