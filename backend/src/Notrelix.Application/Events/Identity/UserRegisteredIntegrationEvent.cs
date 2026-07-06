namespace Notrelix.Application.Events.Identity;

[EventName("identity.user-registered", Version = 2)]
public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    Guid? AccountId,
    string Email,
    string DisplayName,
    Guid? ActorUserId,
    Guid? SourceEventId,
    Guid CorrelationId = default,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    messageName: "identity.user-registered",
    schemaVersion: 2,
    sourceEventId: SourceEventId,
    accountId: AccountId,
    workspaceId: null,
    actorUserId: ActorUserId,
    correlationId: CorrelationId,
    causationId: CausationId,
    occurredAt: OccurredAt);
