namespace Notrelix.Domain.Billing.Subscriptions.Events;

public sealed record SubscriptionPastDueDomainEvent(
    Guid WorkspaceId,
    Guid SubscriptionId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
