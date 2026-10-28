namespace Notrelix.Application.Events.WorkManagement;

/// <summary>An Item left one Board for another in the same Workspace and landed in the target Board's group.</summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("work-management.board-item-moved-to-board", Version = 1)]
public sealed record BoardItemMovedToBoardIntegrationEvent(
    Guid EventId,
    Guid AccountIdValue,
    Guid WorkspaceIdValue,
    Guid ItemId,
    Guid FromBoardId,
    Guid ToBoardId,
    Guid FromGroupId,
    Guid ToGroupId,
    string ItemKey,
    long Revision,
    Guid MovedBy,
    Guid CorrelationId,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default)
    : IntegrationEvent(
        EventId,
        "work-management.board-item-moved-to-board",
        1,
        CorrelationId,
        SourceEventId,
        AccountIdValue,
        WorkspaceIdValue,
        MovedBy,
        CausationId,
        OccurredAt);
