namespace Notrelix.Domain.Billing.Payments;

public enum InvoiceStatus
{
    Draft,
    Open,
    Paid,
    Uncollectible,
    Void
}
