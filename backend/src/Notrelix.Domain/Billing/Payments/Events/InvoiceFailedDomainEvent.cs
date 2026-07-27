using Notrelix.Domain.Billing.Common;
namespace Notrelix.Domain.Billing.Payments.Events;

[EventName("billing.invoice-failed")]
public sealed record InvoiceFailedDomainEvent(
    Guid AccountId,
    Guid InvoiceId,
    Guid? WorkspaceId,
    string Error,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
