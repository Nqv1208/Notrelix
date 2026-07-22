namespace Notrelix.Domain.Billing.Payments.Events;

public sealed record InvoiceSoftDeletedDomainEvent(
    Guid AccountId,
    Guid? WorkspaceId,
    Guid InvoiceId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
