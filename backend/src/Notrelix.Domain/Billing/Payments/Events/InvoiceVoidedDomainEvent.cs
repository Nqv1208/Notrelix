namespace Notrelix.Domain.Billing.Payments.Events;

public sealed record InvoiceVoidedDomainEvent(
    Guid InvoiceId,
    Guid WorkspaceId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
