using Notrelix.Domain.Billing.Common;
namespace Notrelix.Domain.Billing.Payments.Events;

[EventName("billing.invoice-created")]
public sealed record InvoiceCreatedDomainEvent(
    Guid AccountId,
    Guid InvoiceId,
    Guid? WorkspaceId,
    Money Amount,
    DateTimeOffset DueAt,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
