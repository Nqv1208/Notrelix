using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Payments.Events;

public sealed record InvoiceVoidedEvent(
    Guid InvoiceId,
    Guid WorkspaceId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
