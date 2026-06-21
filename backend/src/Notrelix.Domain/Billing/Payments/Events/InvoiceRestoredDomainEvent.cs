using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Payments.Events;

public sealed record InvoiceRestoredDomainEvent(
    Guid WorkspaceId,
    Guid InvoiceId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RestoredBy);
