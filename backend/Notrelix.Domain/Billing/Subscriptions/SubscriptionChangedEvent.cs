using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Billing.Subscriptions;

public sealed record SubscriptionChangedEvent(
    Guid WorkspaceId,
    Guid OldPlanId,
    Guid NewPlanId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
