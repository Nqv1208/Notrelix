namespace Notrelix.Domain.Billing.Payments.Events;

public sealed record InvoiceCreatedDomainEvent(
    Guid AccountId,
    Guid InvoiceId,
    Guid WorkspaceId,
    Money Amount,
    DateTimeOffset DueAt,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, null);
