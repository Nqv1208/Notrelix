using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Subscriptions;

public sealed record SubscriptionRenewedEvent(
    Guid WorkspaceId,
    Guid SubscriptionId,
    DateTimeOffset NewPeriodStart,
    DateTimeOffset NewPeriodEnd,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
