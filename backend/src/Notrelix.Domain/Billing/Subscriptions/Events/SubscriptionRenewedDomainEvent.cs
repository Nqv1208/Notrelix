namespace Notrelix.Domain.Billing.Subscriptions.Events;

public sealed record SubscriptionRenewedDomainEvent(
    Guid WorkspaceId,
    Guid SubscriptionId,
    DateTimeOffset NewPeriodStart,
    DateTimeOffset NewPeriodEnd,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
