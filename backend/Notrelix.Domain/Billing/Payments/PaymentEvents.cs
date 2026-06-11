using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Payments;

public record InvoiceIssuedEvent(Guid InvoiceId, Guid WorkspaceId, Money Amount) : DomainRecordEvent;
public record InvoicePaidEvent(Guid InvoiceId, Guid WorkspaceId) : DomainRecordEvent;
public record InvoiceFailedEvent(Guid InvoiceId, Guid WorkspaceId, string Error) : DomainRecordEvent;
