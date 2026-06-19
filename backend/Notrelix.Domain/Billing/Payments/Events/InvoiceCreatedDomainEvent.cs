using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Billing.Payments.Events;

public sealed record InvoiceCreatedDomainEvent(
    Guid InvoiceId,
    Guid WorkspaceId,
    Money Amount,
    DateTimeOffset DueAt,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
