namespace Notrelix.Application.Events.WorkManagement;

/// <summary>A person was removed from an Item's assignees; UnassignedBy is who removed them.</summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("work-management.board-item-member-unassigned", Version = 1)]
public sealed record BoardItemMemberUnassignedIntegrationEvent(
    Guid EventId,
    Guid AccountIdValue,
    Guid WorkspaceIdValue,
    Guid ItemId,
    Guid UnassignedUserId,
    Guid UnassignedBy,
    Guid CorrelationId,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default)
    : IntegrationEvent(
        EventId,
        "work-management.board-item-member-unassigned",
        1,
        CorrelationId,
        SourceEventId,
        AccountIdValue,
        WorkspaceIdValue,
        UnassignedBy,
        CausationId,
        OccurredAt);
