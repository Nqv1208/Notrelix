using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Billing.Abstractions;
using Notrelix.Application.Features.Billing.Capacity;
using Notrelix.Application.Features.Billing.Public.Capacity;
using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Domain.Billing;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Usage;
using Notrelix.Domain.Common.Exceptions;
using BusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Tests.Features.Billing;

/// <summary>
/// TAC-BI-FLOW-02/03 + TAC-BI-008 — the Billing-owned hard-capacity action
/// behavior at the Application seam: first consume/reserve, same-operation
/// replay (dedup by LogicalOperationId), conflicting replay (deterministic
/// conflict, no mutation), release/compensation, no-entitlement fail-closed,
/// over-capacity rejection, and Account/Workspace scope isolation.
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
    public async Task Consume_WithoutEntitlement_FailsClosed_WithZeroCeiling()
    {
        SetupFacts(limit: null); // no entitlement fact

        var ex = await _sut.Invoking(s => s.ConsumeAsync(
                new ConsumeCapacityRequest(Op()), CancellationToken.None))
            .Should().ThrowAsync<BusinessRuleException>();

        ex.Which.RuleCode.Should().Be(BillingRuleCodes.Billing_Usage_FeatureLimitExceeded);
        var usage = _addedUsage.Single();
        usage.HardLimit.Should().Be(0, "a missing entitlement fails closed to a zero ceiling");
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
    public async Task Consume_SameLogicalOperationId_AcrossWorkspaces_IsNotDeduplicated()
    {
        var otherWorkspace = Guid.CreateVersion7();
        _factsMock
            .Setup(f => f.GetCapabilityAsync(
                AccountId, otherWorkspace, BillingCapabilityCode.AutomationRule, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingCapabilityFact(IsAvailable: true, Limit: 5, Used: 0, Remaining: 5));
        var opId = Guid.CreateVersion7();
        _ledger.Add(Ledger(opId, 1, "rule-A", WorkspaceId));

        var result = await _sut.ConsumeAsync(
            new ConsumeCapacityRequest(Op(logicalOperationId: opId, workspaceId: otherWorkspace)),
            CancellationToken.None);

        result.AlreadyConsumed.Should().BeFalse("dedup is scoped to the Account/Workspace pair");
        result.Remaining.Should().Be(4);
        _addedUsage.Single().WorkspaceId.Should().Be(otherWorkspace);
    }
}