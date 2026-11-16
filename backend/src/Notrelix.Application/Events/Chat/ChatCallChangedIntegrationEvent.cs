namespace Notrelix.Application.Events.Chat;

/// <summary>
/// A call of a space changed: it started, was answered, someone joined or left, a participant changed their media state, or it ended
/// (SPEC-CHAT-MEDIA-CALLS section 7). State, Kind and EndReason use the wire spelling of the call. RingsMembers is true while the call
/// rings a direct or group conversation, so the invited people get the frame on their own stream. It carries ids, never a join token.
/// </summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("chat.call.changed", Version = 1)]
public sealed record ChatCallChangedIntegrationEvent(
    Guid EventId,
    Guid? AccountId,
    Guid? WorkspaceId,
    Guid SpaceId,
    Guid CallId,
    Guid InitiatorId,
    string State,
    string Kind,
    string? EndReason,
    bool RingsMembers,
    long Version,
    IReadOnlyList<Guid> ParticipantIds,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "chat.call.changed",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: null,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt
);
