using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.CQRS;

namespace Notrelix.Infrastructure.Billing;

public sealed class DevNullEntitlementChecker : IEntitlementChecker
{
    public Task<bool> CheckEntitlementAsync(Guid workspaceId, FeatureCode feature, int amount, CancellationToken cancellationToken)
        => Task.FromResult(true);
}
