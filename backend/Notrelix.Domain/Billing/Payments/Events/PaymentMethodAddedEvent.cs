using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Payments.Events;

public sealed record PaymentMethodAddedEvent(
    Guid WorkspaceId,
    Guid PaymentMethodId,
    PaymentProvider Provider,
    string Last4,
    string Brand,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
