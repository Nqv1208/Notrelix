using Notrelix.Domain.Billing.Common;
namespace Notrelix.Domain.Billing.Payments.Events;

[EventName("billing.invoice-voided")]
public sealed record InvoiceVoidedDomainEvent(
    Guid AccountId,
    Guid InvoiceId,
    Guid? WorkspaceId,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
