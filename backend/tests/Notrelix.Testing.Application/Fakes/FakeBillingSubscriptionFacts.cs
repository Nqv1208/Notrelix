using Notrelix.Application.Features.Billing.Public.Subscription;

namespace Notrelix.Testing.Application.Fakes;

/// <summary>
/// Configurable test double for the Billing subscription decision seam. Used by
/// authorization-facts provider construction sites that never gate a request on
/// subscription (so <see cref="SatisfiesRequirementAsync"/> is never invoked),
/// and by composition tests that assert the provider forwards the account and
/// minimum tier to Billing and adopts exactly the returned boolean.
/// </summary>
public sealed class FakeBillingSubscriptionFacts : IBillingSubscriptionFacts
{
    private readonly bool _satisfied;

    public FakeBillingSubscriptionFacts(bool satisfied = true)
    {
        _satisfied = satisfied;
    }

    public int CallCount { get; private set; }

    public Guid? LastAccountId { get; private set; }

    public string? LastMinimumTier { get; private set; }

    public Task<bool> SatisfiesRequirementAsync(
        Guid accountId,
        string? minimumTier,
        CancellationToken cancellationToken)
    {
        CallCount++;
        LastAccountId = accountId;
        LastMinimumTier = minimumTier;
        return Task.FromResult(_satisfied);
    }
}
