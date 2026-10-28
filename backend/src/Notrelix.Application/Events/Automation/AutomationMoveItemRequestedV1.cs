namespace Notrelix.Application.Events.Automation;

[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("automation.move-item-requested", Version = 1)]
public sealed record AutomationMoveItemRequestedV1(
    Guid EventId,
    Guid ExecutionId,
    Guid RuleId,
    Guid? AccountId,
    Guid? WorkspaceId,
    Guid? ActorUserId,
    DateTimeOffset OccurredAt,
    Guid CorrelationId,
    Guid? CausationId = null,
    // AU-1 (additive, nullable): the item the trigger fact was about, which the action moves.
    // Messages staged before it was added carry null and keep the rule's configured item.
    Guid? ItemId = null
) : IntegrationEvent(
    eventId: EventId,
    messageName: "automation.move-item-requested",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
