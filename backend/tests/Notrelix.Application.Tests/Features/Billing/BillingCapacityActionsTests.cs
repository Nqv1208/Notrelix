using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Application.Features.Billing.Capacity;
using Notrelix.Application.Features.Billing.Public.Capacity;
using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Domain.Billing;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Usage;
using BusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Tests.Features.Billing;

/// <summary>
/// TAC-BI-FLOW-02/03 + TAC-BI-008 — the Billing-owned hard-capacity action
/// behavior at the Application seam: first consume/reserve, same-operation
/// replay and conflicting replay (global LogicalOperationId identity),
/// release/compensation, fail-closed decision keyed on IsAvailable, lazy
/// effective-limit reconciliation, over-capacity rejection, and Remaining
/// clamping at zero after a downgrade.
/// </summary>
public class BillingCapacityActionsTests
{
    private static readonly DateTimeOffset TestNow = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    private readonly Mock<IBillingDbContext> _contextMock = new();
    private readonly Mock<IBillingCapabilityFacts> _factsMock = new();
    private readonly BillingCapacityActions _sut;

    private readonly List<FeatureUsageLedger> _ledger = [];
    private readonly List<WorkspaceFeatureUsage> _usage = [];
    private readonly List<FeatureUsageLedger> _addedLedger = [];
    private readonly List<WorkspaceFeatureUsage> _addedUsage = [];

    public BillingCapacityActionsTests()
    {
        _contextMock.Setup(c => c.FeatureUsageLedger)
            .Returns(DbSetOf(_ledger, e => e.Add(It.IsAny<FeatureUsageLedger>()), _addedLedger).Object);
        _contextMock.Setup(c => c.WorkspaceFeatureUsages)
            .Returns(DbSetOf(_usage, u => u.Add(It.IsAny<WorkspaceFeatureUsage>()), _addedUsage).Object);
        _contextMock
            .Setup(c => c.GetOrCreateWorkspaceFeatureUsageAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<decimal?>(), It.IsAny<decimal?>(),
                It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((
                Guid accountId, Guid workspaceId, string capabilityCode,
                decimal? hardLimit, decimal? softLimit,
                Guid actorUserId, DateTimeOffset occurredAt, CancellationToken _) =>
            {
                var feature = FeatureCode.Create(capabilityCode);
                var existing = _usage.FirstOrDefault(u =>
                    u.AccountId == accountId && u.WorkspaceId == workspaceId && u.Feature == feature);
                if (existing is not null)
                    return existing;

                var created = WorkspaceFeatureUsage.Create(
                    accountId, workspaceId, feature, currentUsage: 0, hardLimit, softLimit, occurredAt);
                _usage.Add(created);
                _addedUsage.Add(created);
                return created;
            });
        _sut = new BillingCapacityActions(_contextMock.Object, _factsMock.Object);
    }

    private static Mock<DbSet<T>> DbSetOf<T>(
        List<T> data,
        Expression<Action<DbSet<T>>> addExpression,
        List<T> added)
        where T : class
    {
        var mock = TestBillingDbSet.Create(data);
        mock.Setup(addExpression).Callback<T>(added.Add);
        return mock;
    }

    private static BillingCapacityOperationIdentity Op(
        string capabilityCode = BillingCapabilityCode.AutomationRule,
        decimal amount = 1,
        Guid? logicalOperationId = null,
        Guid? workspaceId = null,
        string sourceResource = "rule-A")
        => new(
            AccountId,
            workspaceId ?? WorkspaceId,
            capabilityCode,
            amount,
            logicalOperationId ?? Guid.CreateVersion7(),
            sourceResource,
            UserId,
            TestNow);

    private static FeatureUsageLedger Ledger(
        Guid logicalOperationId,
        decimal delta,
        string? sourceResource,
        Guid? workspaceId = null)
        => FeatureUsageLedger.Create(
            AccountId,
            workspaceId ?? WorkspaceId,
            BillingCapabilityCode.AutomationRule,
            delta,
            UserId,
            sourceResource,
            note: null,
            TestNow,
            logicalOperationId);

    private static WorkspaceFeatureUsage Usage(
        decimal currentUsage,
        decimal? hardLimit = 5,
        Guid? workspaceId = null)
        => WorkspaceFeatureUsage.Create(
            AccountId,
            workspaceId ?? WorkspaceId,
            FeatureCode.Create(BillingCapabilityCode.AutomationRule),
            currentUsage,
            hardLimit,
            hardLimit,
            TestNow);

    private void SetupFacts(int? limit)
        => _factsMock
            .Setup(f => f.GetCapabilityAsync(
                AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(limit.HasValue
                ? new BillingCapabilityFact(IsAvailable: true, Limit: limit, Used: 0, Remaining: limit)
                : null);

    /// <summary>
    /// Real producer shape for an unavailable grant: the capability provider
    /// never returns null — a missing/expired/zero entitlement surfaces as
    /// IsAvailable=false with Limit=null and the actual ledger sum as Used.
    /// </summary>
    private void SetupUnavailableFact()
        => _factsMock
            .Setup(f => f.GetCapabilityAsync(
                AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingCapabilityFact(IsAvailable: false, Limit: null, Used: 1, Remaining: null));

    private void SetupUsage(params WorkspaceFeatureUsage[] usage) => _usage.AddRange(usage);

    [Fact]
    public async Task Consume_FirstTime_SeedsUsageFromFact_RecordsLedger_ReturnsRemaining()
    {
        SetupFacts(limit: 5);

        var result = await _sut.ConsumeAsync(new ConsumeCapacityRequest(Op()), CancellationToken.None);

        result.AlreadyConsumed.Should().BeFalse();
        result.Remaining.Should().Be(4);
        _addedUsage.Should().HaveCount(1);
        var usage = _addedUsage.Single();
        usage.HardLimit.Should().Be(5);
        usage.CurrentUsage.Should().Be(1);
        var ledger = _addedLedger.Single();
        ledger.Delta.Should().Be(1);
        ledger.Note.Should().Be("capacity-consumed");
        ledger.ReferenceResource.Should().Be("rule-A");
    }

    [Fact]
    public async Task Consume_SameOperationSamePayload_ReturnsAlreadyConsumed_WithoutDoubleEffect()
    {
        SetupFacts(limit: 5);
        SetupUsage(Usage(1));
        var opId = Guid.CreateVersion7();
        _ledger.Add(Ledger(opId, 1, "rule-A")); // committed first consume

        var replay = await _sut.ConsumeAsync(
            new ConsumeCapacityRequest(Op(logicalOperationId: opId)), CancellationToken.None);

        replay.AlreadyConsumed.Should().BeTrue();
        replay.Remaining.Should().Be(4);
        _addedLedger.Should().HaveCount(0, "the replay must not record a second effect");
    }

    [Fact]
    public async Task Consume_SameOperationConflictingPayload_ThrowsCapacityConflict_WithoutMutation()
    {
        SetupFacts(limit: 5);
        var opId = Guid.CreateVersion7();
        _ledger.Add(Ledger(opId, 1, "rule-A"));

        var conflicting = await _sut.Invoking(s => s.ConsumeAsync(
                new ConsumeCapacityRequest(Op(logicalOperationId: opId, sourceResource: "rule-B")),
                CancellationToken.None))
            .Should().ThrowAsync<CapacityOperationConflictException>();

        conflicting.Which.Message.Should().Contain(opId.ToString());
        _addedLedger.Should().BeEmpty();
        _addedUsage.Should().BeEmpty();
    }

    [Fact]
    public async Task Consume_WithUnavailableFact_FailsClosed_WithZeroCeiling()
    {
        SetupUnavailableFact(); // IsAvailable=false, Limit=null — the real producer shape

        var ex = await _sut.Invoking(s => s.ConsumeAsync(
                new ConsumeCapacityRequest(Op()), CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>();

        ex.Which.RuleCode.Should().Be(BillingRuleCodes.Billing_Usage_FeatureLimitExceeded);
        var usage = _addedUsage.Single();
        usage.HardLimit.Should().Be(0, "an unavailable grant fails closed to a zero ceiling");
        _addedLedger.Should().BeEmpty();
    }

    [Fact]
    public async Task Consume_WithMissingFact_FailsClosed_WithZeroCeiling()
    {
        _factsMock
            .Setup(f => f.GetCapabilityAsync(
                AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingCapabilityFact?)null); // a replacement provider returns no fact at all

        var ex = await _sut.Invoking(s => s.ConsumeAsync(
                new ConsumeCapacityRequest(Op()), CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>();

        ex.Which.RuleCode.Should().Be(BillingRuleCodes.Billing_Usage_FeatureLimitExceeded);
        _addedUsage.Single().HardLimit.Should().Be(0, "a missing fact fails closed to a zero ceiling");
        _addedLedger.Should().BeEmpty();
    }

    [Fact]
    public async Task Consume_WhenCapacityExhausted_ThrowsFeatureLimitExceeded_WithoutLedgerEffect()
    {
        SetupFacts(limit: 5);
        SetupUsage(Usage(5));

        var ex = await _sut.Invoking(s => s.ConsumeAsync(
                new ConsumeCapacityRequest(Op()), CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>();

        ex.Which.RuleCode.Should().Be(BillingRuleCodes.Billing_Usage_FeatureLimitExceeded);
        _addedLedger.Should().BeEmpty();
    }

    [Fact]
    public async Task Consume_WithUnlimitedFact_SeedsNullCeiling_ConsumesWithoutBound()
    {
        _factsMock
            .Setup(f => f.GetCapabilityAsync(
                AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingCapabilityFact(IsAvailable: true, Limit: null, Used: null, Remaining: null));

        var result = await _sut.ConsumeAsync(new ConsumeCapacityRequest(Op()), CancellationToken.None);

        result.AlreadyConsumed.Should().BeFalse();
        result.Remaining.Should().BeNull("an unlimited grant has no numeric ceiling");
        _addedUsage.Single().HardLimit.Should().BeNull();
    }

    [Fact]
    public async Task Consume_AfterLimitDowngrade_WithinRemaining_ReconcilesCeiling_AndSucceeds()
    {
        SetupFacts(limit: 2);
        SetupUsage(Usage(currentUsage: 1)); // previously financed at limit 5

        var result = await _sut.ConsumeAsync(new ConsumeCapacityRequest(Op()), CancellationToken.None);

        result.AlreadyConsumed.Should().BeFalse();
        result.Remaining.Should().Be(0, "the surviving headroom after reconciliation is exactly zero");
        _addedLedger.Should().HaveCount(1);
        _addedLedger.Single().Delta.Should().Be(1);
        var reconciled = _usage.Single();
        reconciled.HardLimit.Should().Be(2, "the effective limit reconciles lazily to the current grant");
        reconciled.CurrentUsage.Should().Be(2);
    }

    [Fact]
    public async Task Consume_AfterLimitDowngrade_BelowCurrentUsage_DeniesNewConsumption_RetainsUsage()
    {
        SetupFacts(limit: 2);
        SetupUsage(Usage(currentUsage: 3)); // committed usage already above the new grant

        var ex = await _sut.Invoking(s => s.ConsumeAsync(
                new ConsumeCapacityRequest(Op()), CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>();

        ex.Which.RuleCode.Should().Be(BillingRuleCodes.Billing_Usage_FeatureLimitExceeded);
        var reconciled = _usage.Single();
        reconciled.HardLimit.Should().Be(2, "the ceiling reconciles down to the new grant");
        reconciled.CurrentUsage.Should().Be(3, "a downgrade never deletes committed usage — transient over-limit is retained");
        _addedLedger.Should().BeEmpty();
    }

    [Fact]
    public async Task Consume_AfterLimitUpgrade_ReconcilesCeilingUp()
    {
        SetupFacts(limit: 10);
        SetupUsage(Usage(currentUsage: 1));

        var result = await _sut.ConsumeAsync(new ConsumeCapacityRequest(Op()), CancellationToken.None);

        result.Remaining.Should().Be(8, "the ceiling reconciles up to the enlarged grant");
        _usage.Single().HardLimit.Should().Be(10);
    }

    [Fact]
    public async Task Consume_WhenGrantedUnlimited_AfterFiniteLimit_BecomesUnbounded()
    {
        _factsMock
            .Setup(f => f.GetCapabilityAsync(
                AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingCapabilityFact(IsAvailable: true, Limit: null, Used: 1, Remaining: null));
        SetupUsage(Usage(currentUsage: 1));

        var result = await _sut.ConsumeAsync(new ConsumeCapacityRequest(Op()), CancellationToken.None);

        result.AlreadyConsumed.Should().BeFalse();
        result.Remaining.Should().BeNull("an unlimited grant clears the numeric ceiling");
        _usage.Single().HardLimit.Should().BeNull();
    }

    [Fact]
    public async Task Release_CompensatesConsumption_RestoresCapacity_RecordsNegativeDelta()
    {
        SetupFacts(limit: 5);
        SetupUsage(Usage(1));

        var result = await _sut.ReleaseAsync(new ReleaseCapacityRequest(Op()), CancellationToken.None);

        result.AlreadyReleased.Should().BeFalse();
        result.Remaining.Should().Be(5);
        var ledger = _addedLedger.Single();
        ledger.Delta.Should().Be(-1);
        ledger.Note.Should().Be("capacity-released");
    }

    [Fact]
    public async Task Release_SameOperationTwice_ReturnsAlreadyReleased_NoSecondEffect()
    {
        SetupFacts(limit: 5);
        SetupUsage(Usage(0));
        var opId = Guid.CreateVersion7();
        _ledger.Add(Ledger(opId, -1, "rule-A"));

        var request = new ReleaseCapacityRequest(Op(logicalOperationId: opId));
        var first = await _sut.ReleaseAsync(request, CancellationToken.None);
        var replay = await _sut.ReleaseAsync(request, CancellationToken.None);

        first.AlreadyReleased.Should().BeTrue();
        replay.AlreadyReleased.Should().BeTrue();
        replay.Remaining.Should().Be(5);
        _addedLedger.Should().HaveCount(0);
    }

    [Fact]
    public async Task Release_WithNoUsageRow_ReturnsAlreadyReleased_WithNullRemaining()
    {
        var result = await _sut.ReleaseAsync(new ReleaseCapacityRequest(Op()), CancellationToken.None);

        result.AlreadyReleased.Should().BeTrue();
        result.Remaining.Should().BeNull();
        _addedLedger.Should().BeEmpty();
    }

    [Fact]
    public async Task Release_SameOperationConflictingPayload_ThrowsCapacityConflict()
    {
        var opId = Guid.CreateVersion7();
        _ledger.Add(Ledger(opId, -1, "rule-A"));

        await _sut.Invoking(s => s.ReleaseAsync(
                new ReleaseCapacityRequest(Op(logicalOperationId: opId, sourceResource: "rule-B")),
                CancellationToken.None))
            .Should().ThrowAsync<CapacityOperationConflictException>();
        _addedLedger.Should().BeEmpty();
    }

    [Fact]
    public async Task Release_BelowZero_ThrowsCannotReleaseBelowZero_NoLedgerEffect()
    {
        SetupFacts(limit: 5);
        SetupUsage(Usage(0));

        var ex = await _sut.Invoking(s => s.ReleaseAsync(
                new ReleaseCapacityRequest(Op()), CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>();

        ex.Which.RuleCode.Should().Be(BillingRuleCodes.Billing_Usage_CannotReleaseBelowZero);
        _addedLedger.Should().BeEmpty();
    }

    [Fact]
    public async Task Release_AfterLimitDowngradeBelowUsage_RetainsUsage_ReportsZeroRemaining()
    {
        SetupFacts(limit: 2);
        SetupUsage(Usage(currentUsage: 3)); // over the new grant, from committed history

        var result = await _sut.ReleaseAsync(new ReleaseCapacityRequest(Op()), CancellationToken.None);

        result.AlreadyReleased.Should().BeFalse();
        result.Remaining.Should().Be(0, "remaining is clamped at zero after a downgrade below usage");
        var reconciled = _usage.Single();
        reconciled.HardLimit.Should().Be(2);
        reconciled.CurrentUsage.Should().Be(2, "release still reduces committed usage below the downgraded ceiling");
    }

    [Fact]
    public async Task Consume_SameLogicalOperationId_AcrossWorkspaces_IsDeterministicConflict()
    {
        var otherWorkspace = Guid.CreateVersion7();
        var opId = Guid.CreateVersion7();
        _ledger.Add(Ledger(opId, 1, "rule-A", WorkspaceId));

        await _sut.Invoking(s => s.ConsumeAsync(
                new ConsumeCapacityRequest(Op(logicalOperationId: opId, workspaceId: otherWorkspace)),
                CancellationToken.None))
            .Should().ThrowAsync<CapacityOperationConflictException>(
                "LogicalOperationId is global identity — a reused id with any different payload, including another scope, conflicts");

        _addedUsage.Should().BeEmpty();
        _addedLedger.Should().BeEmpty();
    }
}