using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Billing.Payments.Events;

public sealed record InvoicePaidDomainEvent(
    Guid InvoiceId,
    Guid WorkspaceId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
