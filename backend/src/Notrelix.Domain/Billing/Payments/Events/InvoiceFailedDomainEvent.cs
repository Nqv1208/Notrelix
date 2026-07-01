namespace Notrelix.Domain.Billing.Payments.Events;

public sealed record InvoiceFailedDomainEvent(
    Guid AccountId,
    Guid InvoiceId,
    Guid WorkspaceId,
    string Error,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
