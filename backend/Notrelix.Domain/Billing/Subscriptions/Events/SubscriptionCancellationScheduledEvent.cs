using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Subscriptions.Events;

public sealed record SubscriptionCancellationScheduledEvent(
    Guid WorkspaceId,
    Guid SubscriptionId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, UpdatedBy);
