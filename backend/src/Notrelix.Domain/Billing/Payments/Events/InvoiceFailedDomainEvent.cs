namespace Notrelix.Domain.Billing.Payments.Events;

public sealed record InvoiceFailedDomainEvent(
    Guid InvoiceId,
    Guid WorkspaceId,
    string Error,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
