using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Subscriptions.Events;

public sealed record SubscriptionExpiredEvent(
    Guid WorkspaceId,
    Guid SubscriptionId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
