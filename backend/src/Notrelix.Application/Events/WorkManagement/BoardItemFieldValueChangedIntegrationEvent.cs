namespace Notrelix.Application.Events.WorkManagement;

[EventName("board.item.field_value.changed", Version = 1)]
public sealed record BoardItemFieldValueChangedIntegrationEvent(
    Guid ItemId,
    Guid BoardId,
    Guid FieldId,
    Guid? WorkspaceId,
    string? OldValue,
    string? NewValue,
    Guid? ActorUserId = null,
    Guid CorrelationId = default,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    "board.item.field_value.changed",
    1,
    sourceEventId: null,
    accountId: null,
    WorkspaceId,
    ActorUserId,
    CorrelationId,
    CausationId,
    OccurredAt
);
