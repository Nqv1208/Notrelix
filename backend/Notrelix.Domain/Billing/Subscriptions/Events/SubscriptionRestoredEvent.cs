using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Subscriptions.Events;

public sealed record SubscriptionRestoredEvent(
    Guid WorkspaceId,
    Guid SubscriptionId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RestoredBy);
