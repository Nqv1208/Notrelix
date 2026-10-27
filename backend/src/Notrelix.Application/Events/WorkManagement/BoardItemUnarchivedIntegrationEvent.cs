namespace Notrelix.Application.Events.WorkManagement;

/// <summary>An archived Item was brought back to its Board.</summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("work-management.board-item-unarchived", Version = 1)]
public sealed record BoardItemUnarchivedIntegrationEvent(
    Guid EventId,
    Guid AccountIdValue,
    Guid WorkspaceIdValue,
    Guid ItemId,
    Guid BoardId,
    Guid UnarchivedBy,
    Guid CorrelationId,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default)
    : IntegrationEvent(
        EventId,
        "work-management.board-item-unarchived",
        1,
        CorrelationId,
        SourceEventId,
        AccountIdValue,
        WorkspaceIdValue,
        UnarchivedBy,
        CausationId,
        OccurredAt);
