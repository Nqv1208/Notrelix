using Notrelix.Domain.Billing.Common;
namespace Notrelix.Domain.Billing.Payments.Events;

[EventName("billing.invoice-paid")]
public sealed record InvoicePaidDomainEvent(
    Guid AccountId,
    Guid InvoiceId,
    Guid? WorkspaceId,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
