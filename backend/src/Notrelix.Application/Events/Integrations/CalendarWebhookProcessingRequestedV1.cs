namespace Notrelix.Application.Events.Integrations;

/// <summary>
/// TAC v2.6 WAVE-E — durable provider-neutral intent to process a verified
/// inbound calendar webhook receipt under its derived tenant. Produced by the
/// bootstrap handler and persisted atomically with the "Captured" receipt
/// claim in the same outbox transaction; the tenant-scoped processing consumer
/// executes the Integrations Application seam after commit with the REAL
/// derived tenant applied to the RLS session (not an in-memory adoption).
///
/// The message deliberately carries the stable claim identity
/// (<see cref="ReceiptId"/>) and the decrypted-payload provenance
/// (ConnectionId / Provider / ExternalEventId / PayloadHash / ReceivedAt), NOT
/// the raw callback: the payload stays persisted encrypted on the receipt and
/// is decrypted only inside the tenant-scoped consumer. Workspace-scoped so
/// <c>TenantContextConsumeFilter</c> restores the derived tenant before the
/// consumer touches any state.
/// </summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("integrations.calendar-webhook-processing-requested", Version = 1)]
public sealed record CalendarWebhookProcessingRequestedV1(
    Guid EventId,
    Guid ReceiptId,
    Guid ConnectionId,
    string Provider,
    string ExternalEventId,
    string PayloadHash,
    DateTimeOffset ReceivedAt,
    Guid AccountIdValue,
    Guid WorkspaceIdValue,
    DateTimeOffset OccurredAt,
    Guid CorrelationId,
    Guid? SourceEventId = null,
    Guid? CausationId = null)
    : IntegrationEvent(
        EventId,
        "integrations.calendar-webhook-processing-requested",
        1,
        CorrelationId,
        SourceEventId,
        AccountIdValue,
        WorkspaceIdValue,
        actorUserId: null,
        causationId: CausationId,
        occurredAt: OccurredAt);