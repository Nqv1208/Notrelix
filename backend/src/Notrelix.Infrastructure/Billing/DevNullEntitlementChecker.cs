using Notrelix.Application.Common.Entitlements;

namespace Notrelix.Infrastructure.Billing;

public sealed class DevNullEntitlementChecker : IEntitlementChecker
{
    public Task<bool> CheckEntitlementAsync(Guid workspaceId, FeatureCode feature, int amount, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public Task<bool> HasActiveSubscriptionAsync(Guid workspaceId, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public Task<bool> HasSubscriptionTierAsync(Guid workspaceId, string minimumTier, CancellationToken cancellationToken)
        => Task.FromResult(true);
}
