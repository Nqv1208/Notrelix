namespace Notrelix.Application.Common.Entitlements;

public interface IFeatureGateChecker
{
    Task<bool> IsFeatureEnabledAsync(Guid accountId, string featureCode, int amount, CancellationToken cancellationToken);
}
