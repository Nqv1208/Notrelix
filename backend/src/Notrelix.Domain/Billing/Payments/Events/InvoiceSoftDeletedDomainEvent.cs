using Notrelix.Domain.Billing.Common;
namespace Notrelix.Domain.Billing.Payments.Events;

[EventName("billing.invoice-soft-deleted")]
public sealed record InvoiceSoftDeletedDomainEvent(
    Guid AccountId,
    Guid? WorkspaceId,
    Guid InvoiceId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
