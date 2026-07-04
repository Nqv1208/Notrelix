using Notrelix.Application.Common.Entitlements;

namespace Notrelix.Infrastructure.Billing;

public sealed class DevNullFeatureGateChecker : IFeatureGateChecker
{
    public Task<bool> IsFeatureEnabledAsync(Guid accountId, string featureCode, int amount, CancellationToken cancellationToken)
        => Task.FromResult(true);
}
