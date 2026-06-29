namespace Notrelix.Domain.Billing.Subscriptions.Events;

public sealed record SubscriptionCancellationScheduledDomainEvent(
    Guid WorkspaceId,
    Guid SubscriptionId,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, UpdatedBy);
