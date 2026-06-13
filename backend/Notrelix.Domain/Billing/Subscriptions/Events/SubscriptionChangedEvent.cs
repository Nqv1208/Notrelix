using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Subscriptions.Events;

public sealed record SubscriptionChangedEvent(
    Guid WorkspaceId,
    Guid SubscriptionId,
    Guid OldPlanId,
    Guid NewPlanId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
