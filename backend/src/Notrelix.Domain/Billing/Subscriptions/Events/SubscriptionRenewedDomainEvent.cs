using Notrelix.Domain.Billing.Common;
namespace Notrelix.Domain.Billing.Subscriptions.Events;

[EventName("billing.subscription-renewed")]
public sealed record SubscriptionRenewedDomainEvent(
    Guid AccountId,
    Guid? WorkspaceId,
    Guid SubscriptionId,
    DateTimeOffset NewPeriodStart,
    DateTimeOffset NewPeriodEnd,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
