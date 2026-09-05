namespace Notrelix.Application.Events.WorkManagement;

[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("board.item.field_value.changed", Version = 1)]
public sealed record BoardItemFieldValueChangedIntegrationEvent(
    Guid EventId,
    Guid? AccountId,
    Guid ItemId,
    Guid BoardId,
    Guid FieldId,
    Guid? WorkspaceId,
    string? OldValue,
    string? NewValue,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "board.item.field_value.changed",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
