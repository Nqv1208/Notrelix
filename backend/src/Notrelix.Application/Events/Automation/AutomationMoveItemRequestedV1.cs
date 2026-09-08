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
    Guid? CausationId = null
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
