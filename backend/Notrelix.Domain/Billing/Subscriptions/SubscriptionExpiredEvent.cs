using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Subscriptions;

public sealed record SubscriptionExpiredEvent(
    Guid WorkspaceId,
    Guid SubscriptionId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
