namespace Notrelix.Application.Events.Collaboration;

[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("mention.created", Version = 1)]
public sealed record MentionCreatedIntegrationEvent(
    Guid EventId,
    Guid? AccountId,
    Guid MentionId,
    Guid? WorkspaceId,
    string TargetType,
    Guid TargetId,
    Guid MentionedUserId,
    Guid MentionedByUserId,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "mention.created",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
