namespace Notrelix.Domain.Billing.Payments.Events;

public sealed record InvoiceSoftDeletedDomainEvent(
    Guid WorkspaceId,
    Guid InvoiceId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, DeletedBy);
