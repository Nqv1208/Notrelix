namespace Notrelix.Domain.Billing.BillingEvents;

public enum BillingEventType
{
    SubscriptionCreated,
    SubscriptionUpdated,
    SubscriptionDeleted,
    InvoicePaid,
    InvoicePaymentFailed
}
