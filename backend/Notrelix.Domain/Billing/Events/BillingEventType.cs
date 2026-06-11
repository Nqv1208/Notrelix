namespace Notrelix.Domain.Billing.Events;

public enum BillingEventType
{
    SubscriptionCreated,
    SubscriptionUpdated,
    SubscriptionDeleted,
    InvoicePaid,
    InvoicePaymentFailed
}
