namespace Notrelix.Domain.Billing.Payments.Events;

public sealed record InvoiceRestoredDomainEvent(
    Guid AccountId,
    Guid? WorkspaceId,
    Guid InvoiceId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : BillingAccountScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, RestoredBy);
