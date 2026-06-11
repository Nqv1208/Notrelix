using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Billing.Payments;

public sealed record InvoiceFailedEvent(
    Guid InvoiceId,
    Guid WorkspaceId,
    string Error,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
