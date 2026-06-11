using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Billing.Subscriptions;

public sealed record SubscriptionStartedEvent(
    Guid WorkspaceId,
    Guid SubscriptionId,
    Guid PlanId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
