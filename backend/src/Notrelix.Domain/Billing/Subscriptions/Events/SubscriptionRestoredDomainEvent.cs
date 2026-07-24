using Notrelix.Domain.Billing.Common;
namespace Notrelix.Domain.Billing.Subscriptions.Events;

[EventName("billing.subscription-restored")]
public sealed record SubscriptionRestoredDomainEvent(
    Guid AccountId,
    Guid? WorkspaceId,
    Guid SubscriptionId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
