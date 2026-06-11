using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Billing.Payments;

public sealed record InvoiceIssuedEvent(
    Guid InvoiceId,
    Guid WorkspaceId,
    Money Amount,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
