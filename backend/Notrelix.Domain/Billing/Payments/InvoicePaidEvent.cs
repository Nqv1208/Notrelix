using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Billing.Payments;

public sealed record InvoicePaidEvent(
    Guid InvoiceId,
    Guid WorkspaceId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
