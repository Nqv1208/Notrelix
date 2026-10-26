namespace Notrelix.Application.Events.WorkManagement;

/// <summary>A member who waits on an Item asked the people in charge of it for an update.</summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("work-management.board-item-owners-reminded", Version = 1)]
public sealed record BoardItemOwnersRemindedIntegrationEvent(
    Guid EventId,
    Guid AccountIdValue,
    Guid WorkspaceIdValue,
    Guid ItemId,
    Guid BoardId,
    Guid SenderUserId,
    IReadOnlyList<Guid> RecipientUserIds,
    Guid CorrelationId,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default)
    : IntegrationEvent(
        EventId,
        "work-management.board-item-owners-reminded",
        1,
        CorrelationId,
        SourceEventId,
        AccountIdValue,
        WorkspaceIdValue,
        SenderUserId,
        CausationId,
        OccurredAt);
