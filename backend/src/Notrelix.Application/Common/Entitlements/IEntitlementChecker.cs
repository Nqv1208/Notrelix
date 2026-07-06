namespace Notrelix.Application.Common.Entitlements;

public interface IEntitlementChecker
{
    Task<bool> CheckEntitlementAsync(Guid workspaceId, FeatureCode feature, int amount, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if workspace has an active subscription.
    /// </summary>
    Task<bool> HasActiveSubscriptionAsync(Guid workspaceId, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if workspace has at least the specified subscription tier.
    /// </summary>
    Task<bool> HasSubscriptionTierAsync(Guid workspaceId, string minimumTier, CancellationToken cancellationToken);
}
