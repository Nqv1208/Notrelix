using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Payments.Events;

public sealed record InvoiceSoftDeletedDomainEvent(
    Guid WorkspaceId,
    Guid InvoiceId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DeletedBy);
