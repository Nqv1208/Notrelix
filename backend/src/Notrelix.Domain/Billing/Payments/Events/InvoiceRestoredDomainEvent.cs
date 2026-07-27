using Notrelix.Domain.Billing.Common;
namespace Notrelix.Domain.Billing.Payments.Events;

[EventName("billing.invoice-restored")]
public sealed record InvoiceRestoredDomainEvent(
    Guid AccountId,
    Guid? WorkspaceId,
    Guid InvoiceId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
