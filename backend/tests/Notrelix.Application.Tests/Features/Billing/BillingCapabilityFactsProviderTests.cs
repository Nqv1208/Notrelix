using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Application.Features.Billing.Entitlements.Services;
using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Domain.Billing.Entitlements;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Usage;

namespace Notrelix.Application.Tests.Features.Billing;

/// <summary>
/// TAC-BI-001..005 — the Billing-owned capability surface resolves the
/// database-backed entitlement decision without exposing plan tiers,
/// subscriptions, or provider state.
/// </summary>
public class BillingCapabilityFactsProviderTests
{
    private static readonly DateTimeOffset TestNow = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();

    private readonly Mock<IBillingDbContext> _contextMock = new();
    private readonly Mock<IDateTimeProvider> _clockMock = new();
    private readonly BillingCapabilityFactsProvider _sut;

    public BillingCapabilityFactsProviderTests()
    {
        _clockMock.Setup(c => c.UtcNow).Returns(TestNow);
        _sut = new BillingCapabilityFactsProvider(_contextMock.Object, _clockMock.Object);
    }

    private void SetupEntitlements(params Entitlement[] entitlements)
    {
        var mock = TestBillingDbSet.Create(entitlements.ToList());
        _contextMock.Setup(c => c.Entitlements).Returns(mock.Object);
    }

    private void SetupUsage(params FeatureUsageLedger[] ledger)
    {
        var mock = TestBillingDbSet.Create(ledger.ToList());
        _contextMock.Setup(c => c.FeatureUsageLedger).Returns(mock.Object);
    }

    private static Entitlement ActiveEntitlement(int limit, DateTimeOffset? expiresAt = null) =>
        Entitlement.Create(
            AccountId,
            FeatureCode.Create(BillingCapabilityCode.AutomationRule),
            limit,
            EntitlementSource.Subscription,
            TestNow,
            EntitlementTargetScope.Account,
            targetWorkspaceId: null,
            expiresAt: expiresAt);

    private static FeatureUsageLedger Usage(decimal delta) =>
        FeatureUsageLedger.Create(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule,
            delta, Guid.CreateVersion7(), null, null, TestNow);

    [Fact]
    public async Task GetCapability_WithNoEntitlement_IsUnavailable()
    {
        SetupEntitlements();

        var fact = await _sut.GetCapabilityAsync(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, CancellationToken.None);

        fact.Should().NotBeNull();
        fact!.IsAvailable.Should().BeFalse();
        fact.Limit.Should().BeNull();
    }

    [Fact]
    public async Task GetCapability_WithUnlimitedEntitlement_IsAvailableWithoutQuantity()
    {
        SetupEntitlements(ActiveEntitlement(limit: 0));

        var fact = await _sut.GetCapabilityAsync(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, CancellationToken.None);

        fact!.IsAvailable.Should().BeTrue();
        fact.Limit.Should().BeNull("limit 0 means unlimited in the current model");
        fact.Used.Should().BeNull();
        fact.Remaining.Should().BeNull();
    }

    [Fact]
    public async Task GetCapability_WithExpiredEntitlement_IsUnavailable()
    {
        SetupEntitlements(ActiveEntitlement(limit: 5, expiresAt: TestNow.AddMinutes(-1)));

        var fact = await _sut.GetCapabilityAsync(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, CancellationToken.None);

        fact!.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task GetCapability_WithHeadroom_ReturnsQuantitySemantics()
    {
        SetupEntitlements(ActiveEntitlement(limit: 5));
        SetupUsage(Usage(2));

        var fact = await _sut.GetCapabilityAsync(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, CancellationToken.None);

        fact!.IsAvailable.Should().BeTrue();
        fact.Limit.Should().Be(5);
        fact.Used.Should().Be(2);
        fact.Remaining.Should().Be(3);
    }

    [Fact]
    public async Task GetCapability_WhenLimitExhausted_IsUnavailable()
    {
        SetupEntitlements(ActiveEntitlement(limit: 2));
        SetupUsage(Usage(2));

        var fact = await _sut.GetCapabilityAsync(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, CancellationToken.None);

        fact!.IsAvailable.Should().BeFalse();
        fact.Remaining.Should().Be(0);
    }

    [Fact]
    public async Task GetCapability_NeverExposesPlanTierOrProviderState()
    {
        SetupEntitlements(ActiveEntitlement(limit: 5));
        SetupUsage();

        var fact = await _sut.GetCapabilityAsync(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, CancellationToken.None);

        typeof(BillingCapabilityFact).GetProperties().Select(p => p.Name).Should().BeEquivalentTo(
            ["IsAvailable", "Limit", "Used", "Remaining"],
            "the capability fact carries stable capability meaning only");
    }
}
