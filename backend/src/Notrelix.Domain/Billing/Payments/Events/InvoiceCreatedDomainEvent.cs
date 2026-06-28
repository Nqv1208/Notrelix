namespace Notrelix.Domain.Billing.Payments.Events;

public sealed record InvoiceCreatedDomainEvent(
    Guid InvoiceId,
    Guid WorkspaceId,
    Money Amount,
    DateTimeOffset DueAt,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
