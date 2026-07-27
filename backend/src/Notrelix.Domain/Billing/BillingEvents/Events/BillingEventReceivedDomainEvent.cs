namespace Notrelix.Domain.Billing.BillingEvents.Events;

[EventName("billing.billing-event-received")]
public sealed record BillingEventReceivedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BillingEventId,
    string ProviderEventId,
    BillingEventType Type,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
