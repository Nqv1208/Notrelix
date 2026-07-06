namespace Notrelix.Application.Common.Entitlements;

public interface ISubscriptionChecker
{
    Task<bool> HasActiveSubscriptionAsync(Guid accountId, CancellationToken cancellationToken);
    Task<bool> HasMinimumTierAsync(Guid accountId, string minimumTier, CancellationToken cancellationToken);
}
