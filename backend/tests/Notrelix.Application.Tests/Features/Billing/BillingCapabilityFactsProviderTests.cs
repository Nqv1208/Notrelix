using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Application.Features.Billing.Entitlements.Services;
using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Domain.Billing.Entitlements;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Usage;
using Notrelix.Domain.Common;

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
    private static readonly Guid OtherWorkspaceId = Guid.CreateVersion7();

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

    private static Entitlement ActiveEntitlement(
        int limit,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? expiresAt = null,
        bool isUnlimited = false,
        EntitlementTargetScope targetScope = EntitlementTargetScope.Account,
        Guid? targetWorkspaceId = null) =>
        Entitlement.Create(
            AccountId,
            FeatureCode.Create(BillingCapabilityCode.AutomationRule),
            limit,
            EntitlementSource.Subscription,
            createdAt ?? TestNow,
            targetScope,
            targetWorkspaceId,
            expiresAt,
            isUnlimited);

    private static FeatureUsageLedger Usage(decimal delta, Guid workspaceId) =>
        FeatureUsageLedger.Create(
            AccountId, workspaceId, BillingCapabilityCode.AutomationRule,
            delta, Guid.CreateVersion7(), null, null, TestNow);

    /// <summary>
    /// Entitlement.Create does not maintain audit timestamps, so precedence
    /// tests set CreatedAt explicitly to prove ordering that is independent
    /// of identity.
    /// </summary>
    private static Entitlement WithCreatedAt(Entitlement entitlement, DateTimeOffset createdAt)
    {
        typeof(AuditableEntity)
            .GetProperty(nameof(AuditableEntity.CreatedAt))!
            .GetSetMethod(true)!
            .Invoke(entitlement, [createdAt]);
        return entitlement;
    }

    private static Entitlement WithId(Entitlement entitlement, Guid id)
    {
        typeof(Entity)
            .GetProperty(nameof(Entity.Id))!
            .GetSetMethod(true)!
            .Invoke(entitlement, [id]);
        return entitlement;
    }

    [Fact]
    public async Task GetCapability_WithNoEntitlement_IsUnavailable()
    {
        SetupEntitlements();
        SetupUsage();

        var fact = await _sut.GetCapabilityAsync(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, CancellationToken.None);

        fact.Should().NotBeNull();
        fact!.IsAvailable.Should().BeFalse();
        fact.Limit.Should().BeNull();
        fact.Used.Should().Be(0);
        fact.Remaining.Should().BeNull();
    }

    [Fact]
    public async Task GetCapability_MissingGrantStillReportsLedgerUsage()
    {
        // TAC-BI-001 Used authority: history is never lossy. A missing grant
        // reports the actual ledger sum, not zero, so a later owner action is
        // evaluated against real committed usage.
        SetupEntitlements();
        SetupUsage(Usage(2, WorkspaceId));

        var fact = await _sut.GetCapabilityAsync(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, CancellationToken.None);

        fact!.IsAvailable.Should().BeFalse();
        fact.Used.Should().Be(2);
        fact.Remaining.Should().BeNull();
    }

    [Fact]
    public async Task GetCapability_WithExplicitUnlimitedEntitlement_IsAvailableAndStillReportsUsage()
    {
        // BILL-LIMIT-001: unbounded access is carried by the explicit
        // IsUnlimited representation, never inherited from a zero numeric limit.
        // Used authority still holds: an unlimited grant reports the ledger sum.
        SetupEntitlements(ActiveEntitlement(limit: 0, isUnlimited: true));
        SetupUsage(Usage(3, WorkspaceId));

        var fact = await _sut.GetCapabilityAsync(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, CancellationToken.None);

        fact!.IsAvailable.Should().BeTrue();
        fact.Limit.Should().BeNull("an unlimited grant is not a quantified ceiling");
        fact.Used.Should().Be(3, "used is the ledger sum even under an unlimited grant");
        fact.Remaining.Should().BeNull();
    }

    [Fact]
    public async Task GetCapability_WithZeroNumericLimit_IsUnavailable()
    {
        // BILL-LIMIT-001: after the semantic flip a numeric limit of 0 means
        // zero capacity, distinct from an explicit unlimited grant.
        SetupEntitlements(ActiveEntitlement(limit: 0));
        SetupUsage();

        var fact = await _sut.GetCapabilityAsync(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, CancellationToken.None);

        fact!.IsAvailable.Should().BeFalse();
        fact.Limit.Should().Be(0);
        fact.Used.Should().Be(0);
        fact.Remaining.Should().Be(0);
    }

    [Fact]
    public async Task GetCapability_WithExpiredEntitlement_IsUnavailableAndStillReportsUsage()
    {
        SetupEntitlements(ActiveEntitlement(limit: 5, expiresAt: TestNow.AddMinutes(-1)));
        SetupUsage(Usage(2, WorkspaceId));

        var fact = await _sut.GetCapabilityAsync(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, CancellationToken.None);

        fact!.IsAvailable.Should().BeFalse();
        fact.Used.Should().Be(2, "expiry removes the grant, not the ledger history");
    }

    [Fact]
    public async Task GetCapability_WithRevokedEntitlement_IsUnavailableAndStillReportsUsage()
    {
        var revoked = ActiveEntitlement(limit: 5);
        revoked.Revoke(Guid.CreateVersion7(), TestNow);
        SetupEntitlements(revoked);
        SetupUsage(Usage(2, WorkspaceId));

        var fact = await _sut.GetCapabilityAsync(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, CancellationToken.None);

        fact!.IsAvailable.Should().BeFalse("a revoked grant ceases to apply");
        fact.Limit.Should().BeNull();
        fact.Used.Should().Be(2, "revocation removes the grant, not the ledger history");
        fact.Remaining.Should().BeNull();
    }

    [Fact]
    public async Task GetCapability_WithDisabledEntitlement_IsUnavailableAndStillReportsUsage()
    {
        var disabled = ActiveEntitlement(limit: 5);
        disabled.Disable(Guid.CreateVersion7(), TestNow);
        SetupEntitlements(disabled);
        SetupUsage(Usage(2, WorkspaceId));

        var fact = await _sut.GetCapabilityAsync(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, CancellationToken.None);

        fact!.IsAvailable.Should().BeFalse("a disabled grant ceases to apply");
        fact.Limit.Should().BeNull();
        fact.Used.Should().Be(2, "disable removes the grant, not the ledger history");
        fact.Remaining.Should().BeNull();
    }

    [Fact]
    public async Task GetCapability_WithHeadroom_ReturnsQuantitySemantics()
    {
        SetupEntitlements(ActiveEntitlement(limit: 5));
        SetupUsage(Usage(2, WorkspaceId));

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
        SetupUsage(Usage(2, WorkspaceId));

        var fact = await _sut.GetCapabilityAsync(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, CancellationToken.None);

        fact!.IsAvailable.Should().BeFalse();
        fact.Remaining.Should().Be(0);
    }

    [Fact]
    public async Task GetCapability_RequestedAmountBeyondRemaining_IsUnavailable()
    {
        SetupEntitlements(ActiveEntitlement(limit: 5));
        SetupUsage(Usage(3, WorkspaceId));

        var fact = await _sut.GetCapabilityAsync(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 3, CancellationToken.None);

        fact!.IsAvailable.Should().BeFalse("a request consuming beyond Remaining is rejected");
        fact.Remaining.Should().Be(2);
    }

    [Fact]
    public async Task GetCapability_WhenRemainingEqualsRequested_IsAvailable()
    {
        // TAC-BI-001 boundary: Remaining == Requested is the exact equality
        // that must still pass — the request consumes the very last unit.
        SetupEntitlements(ActiveEntitlement(limit: 5));
        SetupUsage(Usage(4, WorkspaceId));

        var fact = await _sut.GetCapabilityAsync(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, CancellationToken.None);

        fact!.IsAvailable.Should().BeTrue("a request equal to the remaining capacity is allowed");
        fact.Remaining.Should().Be(1);
        fact.Limit.Should().Be(5);
    }

    [Fact]
    public async Task GetCapability_WorkspaceGrant_BeatsAccountGrant_EvenIfOlder()
    {
        // TAC-BI-001 precedence: a workspace-targeted grant wins solely by
        // scope, not by age. The account-scoped grant is newer yet loses.
        SetupEntitlements(
            WithCreatedAt(ActiveEntitlement(limit: 5), TestNow),
            WithCreatedAt(ActiveEntitlement(
                limit: 10,
                createdAt: TestNow.AddDays(-2),
                targetScope: EntitlementTargetScope.Workspace,
                targetWorkspaceId: WorkspaceId), TestNow.AddDays(-2)));
        SetupUsage();

        var fact = await _sut.GetCapabilityAsync(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, CancellationToken.None);

        fact!.IsAvailable.Should().BeTrue();
        fact.Limit.Should().Be(10);
        fact.Remaining.Should().Be(10);
    }

    [Fact]
    public async Task GetCapability_NewestGrant_Wins_WithinSameScope()
    {
        // TAC-BI-001 precedence: within one scope the newest grant applies.
        SetupEntitlements(
            WithCreatedAt(ActiveEntitlement(limit: 5), TestNow.AddDays(-3)),
            WithCreatedAt(ActiveEntitlement(limit: 10), TestNow.AddDays(-1)));
        SetupUsage();

        var fact = await _sut.GetCapabilityAsync(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, CancellationToken.None);

        fact!.Limit.Should().Be(10, "within same scope the newest grant wins");
    }

    [Fact]
    public async Task GetCapability_SameCreatedAt_TieBreaksByNewerIdentity()
    {
        // TAC-BI-001 precedence: when CreatedAt ties within a scope, the
        // larger identity wins so the choice stays deterministic and never an
        // arbitrary first row.
        SetupEntitlements(
            WithId(WithCreatedAt(ActiveEntitlement(limit: 5, createdAt: TestNow), TestNow), Guid.Parse("00000000-0000-0000-0000-000000000001")),
            WithId(WithCreatedAt(ActiveEntitlement(limit: 10, createdAt: TestNow), TestNow), Guid.Parse("00000000-0000-0000-0000-000000000003")));
        SetupUsage();

        var fact = await _sut.GetCapabilityAsync(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, CancellationToken.None);

        fact!.Limit.Should().Be(10, "identity is the deterministic tie-break");
    }

    [Fact]
    public async Task GetCapability_ExpiredWorkspaceGrant_DoesNotShadowValidAccountGrant()
    {
        // TAC-BI-001 precedence: the applicable set is reduced before ordering.
        // An expired workspace grant must never shadow a still-valid account grant.
        SetupEntitlements(
            ActiveEntitlement(
                limit: 10,
                createdAt: TestNow.AddDays(-2),
                expiresAt: TestNow.AddMinutes(-1),
                targetScope: EntitlementTargetScope.Workspace,
                targetWorkspaceId: WorkspaceId),
            ActiveEntitlement(limit: 3));
        SetupUsage();

        var fact = await _sut.GetCapabilityAsync(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, CancellationToken.None);

        fact!.IsAvailable.Should().BeTrue();
        fact.Limit.Should().Be(3, "the valid account grant applies, not the expired workspace grant");
    }

    [Fact]
    public async Task GetCapability_Usage_IsWorkspaceIsolated()
    {
        // TAC-BI-001 Used authority: the ledger sum is scoped per (account,
        // workspace, capability). Usage in another workspace must not count here.
        SetupEntitlements(ActiveEntitlement(limit: 5));
        SetupUsage(Usage(4, OtherWorkspaceId));

        var fact = await _sut.GetCapabilityAsync(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, CancellationToken.None);

        fact!.Used.Should().Be(0, "usage in another workspace is not counted");
        fact.Remaining.Should().Be(5);
    }

    [Fact]
    public async Task GetCapability_WorkspaceGrant_InAnotherWorkspace_DoesNotApply()
    {
        SetupEntitlements(ActiveEntitlement(
            limit: 10,
            createdAt: TestNow,
            targetScope: EntitlementTargetScope.Workspace,
            targetWorkspaceId: OtherWorkspaceId));
        SetupUsage();

        var fact = await _sut.GetCapabilityAsync(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, CancellationToken.None);

        fact!.IsAvailable.Should().BeFalse(
            "a workspace grant targets one workspace and does not leak to another");
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