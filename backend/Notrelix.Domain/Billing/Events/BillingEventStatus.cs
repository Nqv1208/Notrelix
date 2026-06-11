namespace Notrelix.Domain.Billing.Events;

public enum BillingEventStatus
{
    Received,
    Processed,
    Failed,
    Ignored
}
