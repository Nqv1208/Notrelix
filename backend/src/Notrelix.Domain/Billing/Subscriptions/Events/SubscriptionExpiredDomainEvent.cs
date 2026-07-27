using Notrelix.Domain.Billing.Common;
namespace Notrelix.Domain.Billing.Subscriptions.Events;

[EventName("billing.subscription-expired")]
public sealed record SubscriptionExpiredDomainEvent(
    Guid AccountId,
    Guid? WorkspaceId,
    Guid SubscriptionId,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
