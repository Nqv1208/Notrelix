using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Subscriptions.Events;

public sealed record SubscriptionPastDueEvent(
    Guid WorkspaceId,
    Guid SubscriptionId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
