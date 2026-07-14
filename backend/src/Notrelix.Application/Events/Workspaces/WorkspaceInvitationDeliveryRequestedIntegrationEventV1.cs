namespace Notrelix.Application.Events.Workspaces;

[EventName("workspaces.invitation-delivery-requested", Version = 1)]
public sealed record WorkspaceInvitationDeliveryRequestedIntegrationEventV1(
    Guid EventId,
    Guid InvitationId,
    Guid? AccountId,
    Guid? WorkspaceId,
    string RecipientEmail,
    string ProtectedToken,
    int HashVersion,
    int TokenGeneration,
    DateTimeOffset ExpiresAt,
    Guid InvitedBy,
    Guid CorrelationId,
    Guid? ActorUserId = null,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    DateTimeOffset OccurredAt = default
) : IntegrationEvent(
    eventId: EventId,
    messageName: "workspaces.invitation-delivery-requested",
    schemaVersion: 1,
    correlationId: CorrelationId,
    sourceEventId: SourceEventId,
    accountId: AccountId,
    workspaceId: WorkspaceId,
    actorUserId: ActorUserId,
    causationId: CausationId,
    occurredAt: OccurredAt);
