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
    DbSet<WorkspaceFeatureUsage> WorkspaceFeatureUsages { get; }
    DbSet<PaymentMethod> PaymentMethods { get; }

    /// <summary>
    /// Atomically seeds the per-scope usage row on first use for a capability:
    /// INSERT ... ON CONFLICT DO NOTHING that materializes CurrentUsage from the
    /// SUM of the committed ledger (so first-use never drifts from history), then
    /// reloads the authoritative row through the scoped DbSet so the winner of a
    /// concurrent first-use race is observed. MUST be invoked inside the same
    /// request transaction as the waiter's SaveChanges (BOUND-TX-003).
    /// </summary>
    Task<WorkspaceFeatureUsage> GetOrCreateWorkspaceFeatureUsageAsync(
        Guid accountId,
        Guid workspaceId,
        string capabilityCode,
        decimal? hardLimit,
        decimal? softLimit,
        Guid actorUserId,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken);
}