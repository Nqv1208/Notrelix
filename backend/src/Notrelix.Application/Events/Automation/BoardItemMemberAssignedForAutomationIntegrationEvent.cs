namespace Notrelix.Application.Events.Automation;

[EventName("automation.board-item-member-assigned", Version = 1)]
public sealed record BoardItemMemberAssignedForAutomationIntegrationEvent(
    Guid EventId,
    Guid AccountIdValue,
    Guid WorkspaceIdValue,
    Guid ItemId,
    Guid AssignedUserId,
    Guid AssignedBy,
    Guid CorrelationId,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default)
    : IntegrationEvent(
        EventId,
        "automation.board-item-member-assigned",
        1,
        CorrelationId,
        SourceEventId,
        AccountIdValue,
        WorkspaceIdValue,
        AssignedBy,
        CausationId,
        OccurredAt);
