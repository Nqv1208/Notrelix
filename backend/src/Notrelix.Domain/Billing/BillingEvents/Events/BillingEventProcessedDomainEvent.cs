namespace Notrelix.Domain.Billing.BillingEvents.Events;

[EventName("billing.billing-event-processed")]
public sealed record BillingEventProcessedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BillingEventId,
    string ProviderEventId,
    BillingEventStatus Status,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
