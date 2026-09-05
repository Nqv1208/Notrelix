namespace Notrelix.Application.Events.Collaboration;

[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("comment.created", Version = 1)]
public sealed record CommentCreatedIntegrationEvent(
    Guid EventId,
    Guid? AccountId,
    Guid CommentId,
    Guid? WorkspaceId,
    string TargetType,
    Guid TargetId,
    Guid AuthorId,
    string Body,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "comment.created",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
