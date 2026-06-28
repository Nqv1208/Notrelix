namespace Notrelix.Domain.Billing.Payments.Events;

public sealed record InvoiceRestoredDomainEvent(
    Guid WorkspaceId,
    Guid InvoiceId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, RestoredBy);
