namespace Notrelix.Application.Events.Integrations;

/// <summary>
/// Durable intent to deliver one signed message to an outgoing webhook connection. It is written to the
/// outbox with the request that caused it, so a delivery is never lost and a redelivery of the message
/// repeats the same delivery identity (<see cref="DeliveryId"/>, sent as `X-Notrelix-Delivery`) which the
/// receiver can use to ignore a repeat. The consumer builds the body, signs it with the connection's
/// secret and records the outcome on the connection; nothing secret travels in the message.
/// </summary>
[IntegrationEventTenantScope(IntegrationEventTenantScope.Workspace)]
[EventName("integrations.webhook-dispatch-requested", Version = 1)]
public sealed record IntegrationWebhookDispatchRequestedV1(
    Guid EventId,
    Guid DeliveryId,
    Guid ConnectionId,
    string DeliveryType,
    Guid AccountIdValue,
    Guid WorkspaceIdValue,
    DateTimeOffset OccurredAt,
    Guid CorrelationId,
    Guid? ActorUserIdValue = null,
    Guid? SourceEventId = null,
    Guid? CausationId = null,
    string? PayloadJson = null)
    : IntegrationEvent(
        EventId,
        "integrations.webhook-dispatch-requested",
        1,
        CorrelationId,
        SourceEventId,
        AccountIdValue,
        WorkspaceIdValue,
        actorUserId: ActorUserIdValue,
        causationId: CausationId,
        occurredAt: OccurredAt)
{
    /// <summary>The delivery a member asked for to check the connection.</summary>
    public const string TestType = "notrelix.test";

    /// <summary>An Item was created on a Board of the Workspace.</summary>
    public const string ItemCreatedType = "item.created";

    /// <summary>An Item was renamed or one of its field values changed (other than Status).</summary>
    public const string ItemUpdatedType = "item.updated";

    /// <summary>An Item's Status field changed.</summary>
    public const string ItemStatusChangedType = "item.status_changed";

    /// <summary>A comment was added to an Item.</summary>
    public const string CommentAddedType = "comment.added";
}
