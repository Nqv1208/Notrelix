using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Billing.Subscriptions;

public sealed record SubscriptionCancelledEvent(
    Guid WorkspaceId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
