using Notrelix.Application.Features.Billing.Public.Subscription;

namespace Notrelix.Infrastructure.Billing;

/// <summary>
/// DevNull mode bypass: reports every subscription requirement as satisfied.
/// Registered only when Billing:Mode=DevNull (guarded to Development/Testing
/// by <c>BillingRegistration</c>); never registered in production.
/// </summary>
public sealed class DevNullBillingSubscriptionFacts : IBillingSubscriptionFacts
{
    public Task<bool> SatisfiesRequirementAsync(
        Guid accountId,
        string? minimumTier,
        CancellationToken cancellationToken) => Task.FromResult(true);
}
