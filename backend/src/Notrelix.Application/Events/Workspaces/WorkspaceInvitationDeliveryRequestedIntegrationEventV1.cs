namespace Notrelix.Application.Events.Workspaces;

[EventName("workspaces.invitation-delivery-requested", Version = 1)]
[EventPiiField("RecipientEmail",
    Purpose = "Address the workspace invitation email to the invitee.",
    ConsumerJustification = "SendInvitationEmailConsumer performs delivery semantics; the invitee address is the delivery target itself and cannot be derived from stable IDs by a downstream context.")]
[EventSensitiveField("ProtectedToken",
    Classification = "protected-single-use-invitation-token",
    Justification = "Encrypted single-use token required to construct the invitation URL inside the delivered email; it is not a raw credential, is versioned via TokenGeneration, and must never be logged by outbox/retry/DLQ diagnostics.")]
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
