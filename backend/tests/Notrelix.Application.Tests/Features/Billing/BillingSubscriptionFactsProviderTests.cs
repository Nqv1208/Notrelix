using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Application.Features.Billing.Subscriptions.Services;
using Notrelix.Domain.Billing.Subscriptions;

namespace Notrelix.Application.Tests.Features.Billing;

/// <summary>
/// DEBT-BILL-002 closure — the Billing-owned subscription decision surface.
/// Billing alone answers "is there an active subscription AND does it meet the
/// requested tier"; the tier comparison authority is the Domain
/// <see cref="SubscriptionTier"/> enum. Consumers (including the authorization
/// pipeline) never re-derive this ladder.
/// </summary>
public class BillingSubscriptionFactsProviderTests
{
    private static readonly DateTimeOffset TestNow = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid OtherAccountId = Guid.CreateVersion7();
    private static readonly Guid Actor = Guid.CreateVersion7();

    private readonly Mock<IBillingDbContext> _contextMock = new();
    private readonly Mock<IDateTimeProvider> _clockMock = new();
    private readonly BillingSubscriptionFactsProvider _sut;

    public BillingSubscriptionFactsProviderTests()
    {
        _clockMock.Setup(c => c.UtcNow).Returns(TestNow);
        _sut = new BillingSubscriptionFactsProvider(_contextMock.Object, _clockMock.Object);
    }

    private void SetupSubscriptions(params Subscription[] subscriptions)
    {
        var mock = TestBillingDbSet.Create(subscriptions.ToList());
        _contextMock.Setup(c => c.Subscriptions).Returns(mock.Object);
    }

    private static Subscription Active(SubscriptionTier tier, Guid? account = null) =>
        Subscription.Create(
            account ?? AccountId, Guid.CreateVersion7(), tier,
            TestNow.AddDays(-10), TestNow.AddDays(20), Actor, TestNow);

    private static Subscription Expired(SubscriptionTier tier) =>
        Subscription.Create(
            AccountId, Guid.CreateVersion7(), tier,
            TestNow.AddDays(-40), TestNow.AddDays(-1), Actor, TestNow.AddDays(-40));

    private static Subscription Canceled(SubscriptionTier tier)
    {
        var sub = Active(tier);
        sub.CancelImmediately(Actor, TestNow);
        return sub;
    }

    [Fact]
    public async Task NoActiveSubscription_Denies()
    {
        SetupSubscriptions();

        var satisfied = await _sut.SatisfiesRequirementAsync(AccountId, "Pro", CancellationToken.None);

        satisfied.Should().BeFalse("no subscription means the requirement is unmet");
    }

    [Fact]
    public async Task ActiveSubscription_NoMinimumTier_Allows()
    {
        SetupSubscriptions(Active(SubscriptionTier.Free));

        var satisfied = await _sut.SatisfiesRequirementAsync(AccountId, minimumTier: null, CancellationToken.None);

        satisfied.Should().BeTrue("any active subscription is sufficient when no tier is required");
    }

    [Fact]
    public async Task ActiveSubscription_EmptyMinimumTier_Allows()
    {
        SetupSubscriptions(Active(SubscriptionTier.Free));

        var satisfied = await _sut.SatisfiesRequirementAsync(AccountId, string.Empty, CancellationToken.None);

        satisfied.Should().BeTrue("an empty minimum tier is treated as no tier requirement");
    }

    [Fact]
    public async Task ActiveLowerTier_RequiresHigher_Denies()
    {
        SetupSubscriptions(Active(SubscriptionTier.Free));

        var satisfied = await _sut.SatisfiesRequirementAsync(AccountId, "Pro", CancellationToken.None);

        satisfied.Should().BeFalse("Free is below the required Pro tier");
    }

    [Fact]
    public async Task ActiveExactTier_Allows()
    {
        SetupSubscriptions(Active(SubscriptionTier.Pro));

        var satisfied = await _sut.SatisfiesRequirementAsync(AccountId, "Pro", CancellationToken.None);

        satisfied.Should().BeTrue("meeting the exact required tier satisfies the gate");
    }

    [Fact]
    public async Task ActiveHigherTier_Allows()
    {
        SetupSubscriptions(Active(SubscriptionTier.Enterprise));

        var satisfied = await _sut.SatisfiesRequirementAsync(AccountId, "Pro", CancellationToken.None);

        satisfied.Should().BeTrue("a tier above the requirement satisfies the gate");
    }

    [Fact]
    public async Task UnknownMinimumTier_Denies()
    {
        SetupSubscriptions(Active(SubscriptionTier.Enterprise));

        var satisfied = await _sut.SatisfiesRequirementAsync(AccountId, "Platinum", CancellationToken.None);

        satisfied.Should().BeFalse("an unknown tier fails closed — no implicit grant");
    }

    [Fact]
    public async Task MinimumTier_IsCaseInsensitive()
    {
        SetupSubscriptions(Active(SubscriptionTier.Pro));

        var satisfied = await _sut.SatisfiesRequirementAsync(AccountId, "pro", CancellationToken.None);

        satisfied.Should().BeTrue("tier matching ignores case at the Billing boundary");
    }

    [Fact]
    public async Task ExpiredSubscription_Denies()
    {
        SetupSubscriptions(Expired(SubscriptionTier.Enterprise));

        var satisfied = await _sut.SatisfiesRequirementAsync(AccountId, "Pro", CancellationToken.None);

        satisfied.Should().BeFalse("a subscription past its current period end is not active");
    }

    [Fact]
    public async Task CanceledSubscription_Denies()
    {
        SetupSubscriptions(Canceled(SubscriptionTier.Enterprise));

        var satisfied = await _sut.SatisfiesRequirementAsync(AccountId, "Pro", CancellationToken.None);

        satisfied.Should().BeFalse("a canceled subscription is not active");
    }

    [Fact]
    public async Task ForeignAccountSubscription_DoesNotSatisfy()
    {
        SetupSubscriptions(Active(SubscriptionTier.Enterprise, account: OtherAccountId));

        var satisfied = await _sut.SatisfiesRequirementAsync(AccountId, "Pro", CancellationToken.None);

        satisfied.Should().BeFalse("subscription scope is the account under evaluation");
    }

    [Fact]
    public async Task MultipleSubscriptions_BestTierWins()
    {
        SetupSubscriptions(Active(SubscriptionTier.Free), Active(SubscriptionTier.Enterprise));

        var satisfied = await _sut.SatisfiesRequirementAsync(AccountId, "Enterprise", CancellationToken.None);

        satisfied.Should().BeTrue("the decision considers all active subscriptions, not an arbitrary row");
    }
}
