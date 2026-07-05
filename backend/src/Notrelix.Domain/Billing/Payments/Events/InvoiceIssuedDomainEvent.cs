namespace Notrelix.Domain.Billing.Payments.Events;

public sealed record InvoiceIssuedDomainEvent(
    Guid AccountId,
    Guid InvoiceId,
    Guid? WorkspaceId,
    Money Amount,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
