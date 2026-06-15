using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Subscriptions.Events;

public sealed record SubscriptionSoftDeletedEvent(
    Guid WorkspaceId,
    Guid SubscriptionId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DeletedBy);
