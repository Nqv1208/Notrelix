namespace Notrelix.Domain.Billing.Subscriptions.Events;

public sealed record SubscriptionSoftDeletedDomainEvent(
    Guid WorkspaceId,
    Guid SubscriptionId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, DeletedBy);
