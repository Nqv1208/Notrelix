namespace Notrelix.Domain.Billing;

public enum BillingEventType
{
    SubscriptionCreated,
    SubscriptionUpdated,
    SubscriptionDeleted,
    InvoicePaid,
    InvoicePaymentFailed
}
