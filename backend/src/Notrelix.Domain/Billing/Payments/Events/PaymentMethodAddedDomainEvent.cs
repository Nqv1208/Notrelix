namespace Notrelix.Domain.Billing.Payments.Events;

[EventName("billing.payment-method-added")]
public sealed record PaymentMethodAddedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid PaymentMethodId,
    PaymentProvider Provider,
    string Last4,
    string Brand,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
