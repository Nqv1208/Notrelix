using Notrelix.Domain.Billing.Customers;
using Notrelix.Domain.Billing.Entitlements;
using Notrelix.Domain.Billing.BillingEvents;
using Notrelix.Domain.Billing.Payments;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Subscriptions;
using Notrelix.Domain.Billing.Usage;
// ReSharper disable InconsistentNaming — intentionally mirrors DbContext member naming

namespace Notrelix.Application.Features.Billing.Abstractions;

public interface IBillingDbContext
{
    DbSet<Plan> Plans { get; }
    DbSet<PlanLimit> PlanLimits { get; }
    DbSet<PlanPrice> PlanPrices { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<SubscriptionItem> SubscriptionItems { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceLineItem> InvoiceLineItems { get; }
    DbSet<BillingEvent> BillingEvents { get; }
    DbSet<BillingCustomer> BillingCustomers { get; }
    DbSet<Entitlement> Entitlements { get; }
    DbSet<UsageMetric> UsageMetrics { get; }
    DbSet<UsageMetricHistory> UsageMetricHistories { get; }
    DbSet<FeatureUsageLedger> FeatureUsageLedger { get; }
    DbSet<PaymentMethod> PaymentMethods { get; }
}