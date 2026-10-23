namespace Notrelix.Application.Events.WorkManagement;

[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("board_item.moved", Version = 2)]
public sealed record BoardItemMovedIntegrationEventV2(
    Guid EventId,
    Guid? AccountId,
    Guid ItemId,
    Guid BoardId,
    Guid? WorkspaceId,
    Guid? OldGroupId,
    Guid? NewGroupId,
    long Revision,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default,
    // WM-V3-EVT-001: the authoritative final ordering key in NewGroupId (additive,
    // nullable; messages produced before it was added deserialize with null).
    string? NewPosition = null,
    // AUT-020 (additive, nullable): set only when an Automation rule moved the item.
    // ActorUserId is then the rule's executor ("automation <rule> on behalf of
    // <executor>"); older messages and user moves deserialize with null.
    Guid? AutomationRuleId = null,
    Guid? AutomationExecutionId = null
) : IntegrationEvent(
    eventId: EventId,
    messageName: "board_item.moved",
    schemaVersion: 2,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);