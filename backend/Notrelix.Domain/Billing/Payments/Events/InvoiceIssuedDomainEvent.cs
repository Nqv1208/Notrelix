using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Billing.Payments.Events;

public sealed record InvoiceIssuedDomainEvent(
    Guid InvoiceId,
    Guid WorkspaceId,
    Money Amount,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
