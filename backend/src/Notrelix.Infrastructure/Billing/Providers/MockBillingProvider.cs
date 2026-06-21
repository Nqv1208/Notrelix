namespace Notrelix.Infrastructure.Billing.Providers;

/// <summary>
/// Skeleton mock billing provider (v4 §13). WorkManagement/Application must not
/// know Stripe — they depend only on an IBillingProvider/IFeatureEntitlementService
/// abstraction. Billing webhooks must verify signature + be idempotent. Not yet wired.
/// </summary>
public sealed class MockBillingProvider
{
    // TODO(v4 §13): implement IBillingProvider (checkout/subscription ops).
    // Real Stripe provider sits alongside; entitlement cache invalidated on change.
}
