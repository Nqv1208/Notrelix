namespace Notrelix.Domain.Billing.BillingEvents.Events;

public sealed record BillingEventProcessedDomainEvent(
    Guid WorkspaceId,
    Guid BillingEventId,
    string ProviderEventId,
    BillingEventStatus Status,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
