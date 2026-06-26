namespace Notrelix.Application.Common.Abstractions;

public interface IEntitlementChecker
{
    Task<bool> CheckEntitlementAsync(Guid workspaceId, FeatureCode feature, int amount, CancellationToken cancellationToken);
}
