namespace Notrelix.Domain.Billing.BillingEvents.Events;

public sealed record BillingEventReceivedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid BillingEventId,
    string ProviderEventId,
    BillingEventType Type,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
