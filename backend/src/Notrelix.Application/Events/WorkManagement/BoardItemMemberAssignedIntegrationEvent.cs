namespace Notrelix.Application.Events.WorkManagement;

[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("work-management.board-item-member-assigned", Version = 1)]
public sealed record BoardItemMemberAssignedIntegrationEvent(
    Guid EventId,
    Guid AccountIdValue,
    Guid WorkspaceIdValue,
    Guid ItemId,
    Guid AssignedUserId,
    Guid AssignedBy,
    Guid CorrelationId,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default,
    // Additive: the board the item is on, so board rules can match the assignment.
    Guid? BoardId = null)
    : IntegrationEvent(
        EventId,
        "work-management.board-item-member-assigned",
        1,
        CorrelationId,
        SourceEventId,
        AccountIdValue,
        WorkspaceIdValue,
        AssignedBy,
        CausationId,
        OccurredAt);
