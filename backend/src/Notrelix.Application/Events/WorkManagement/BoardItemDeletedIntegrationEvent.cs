namespace Notrelix.Application.Events.WorkManagement;

/// <summary>An Item was moved to its Board's deleted list.</summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("work-management.board-item-deleted", Version = 1)]
public sealed record BoardItemDeletedIntegrationEvent(
    Guid EventId,
    Guid AccountIdValue,
    Guid WorkspaceIdValue,
    Guid ItemId,
    Guid BoardId,
    Guid DeletedBy,
    Guid CorrelationId,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default)
    : IntegrationEvent(
        EventId,
        "work-management.board-item-deleted",
        1,
        CorrelationId,
        SourceEventId,
        AccountIdValue,
        WorkspaceIdValue,
        DeletedBy,
        CausationId,
        OccurredAt);
