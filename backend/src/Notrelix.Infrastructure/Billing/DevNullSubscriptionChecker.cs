using Notrelix.Application.Common.Entitlements;

namespace Notrelix.Infrastructure.Billing;

public sealed class DevNullSubscriptionChecker : ISubscriptionChecker
{
    public Task<bool> HasActiveSubscriptionAsync(Guid accountId, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public Task<bool> HasMinimumTierAsync(Guid accountId, string minimumTier, CancellationToken cancellationToken)
        => Task.FromResult(true);
}
