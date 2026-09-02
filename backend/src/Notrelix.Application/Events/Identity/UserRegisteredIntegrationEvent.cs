namespace Notrelix.Application.Events.Identity;

[IntegrationEventTenantScope(IntegrationEventTenantScope.None)]
[EventName("identity.user-registered", Version = 2)]
[EventPiiField("Email",
    Purpose = "Preserve the accepted v2 registration telemetry payload.",
    ConsumerJustification = "UserRegisteredConsumer is a non-tenant Identity telemetry consumer; the field remains in v2 for backward compatibility and is not used for Account/Workspace mutation.")]
[EventPiiField("DisplayName",
    Purpose = "Preserve the accepted v2 registration telemetry payload.",
    ConsumerJustification = "UserRegisteredConsumer is a non-tenant Identity telemetry consumer; the field remains in v2 for backward compatibility and is not used for Account/Workspace mutation.")]
public sealed record UserRegisteredIntegrationEvent(
    Guid EventId,
    Guid UserId,
    string Email,
    string DisplayName,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "identity.user-registered",
    schemaVersion: 2,
    correlationId: CorrelationId,
    sourceEventId: SourceEventId,
    accountId: null,
    workspaceId: null,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt);
