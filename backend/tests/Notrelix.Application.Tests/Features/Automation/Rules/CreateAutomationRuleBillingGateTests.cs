using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Automation.Rules.Commands.CreateAutomationRule;
using Notrelix.Application.Features.Billing.Public.Capacity;
using Notrelix.Application.Features.Billing.Public.Facts;
using Notrelix.Application.Tests.Features.Billing;

namespace Notrelix.Application.Tests.Features.Automation.Rules;

/// <summary>
/// TAC-BI-006/007 — the pinned Billing reference consumer asks the Billing-owned
/// capability surface (never a plan tier) and fails deterministically before
/// any rule mutation when the capability is unavailable.
/// </summary>
public class CreateAutomationRuleBillingGateTests
{
    private static readonly DateTimeOffset TestNow = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    private readonly Mock<IAutomationDbContext> _contextMock = new();
    private readonly Mock<ICurrentRequestContext> _requestContextMock = new();
    private readonly Mock<IBillingCapabilityFacts> _billingMock = new();
    private readonly Mock<IBillingCapacityActions> _billingCapacityMock = new();

    public CreateAutomationRuleBillingGateTests()
    {
        _requestContextMock.Setup(c => c.UserId).Returns(UserId);
        _requestContextMock.Setup(c => c.RequireAccountId()).Returns(AccountId);
        _requestContextMock.Setup(c => c.IsAuthenticated).Returns(true);
        _contextMock.Setup(c => c.AutomationRules).Returns(TestBillingDbSet.Create(new List<Domain.Automation.Rules.AutomationRule>()).Object);
    }

    private CreateAutomationRuleCommandHandler CreateSut()
    {
        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(TestNow);
        return new CreateAutomationRuleCommandHandler(
            _contextMock.Object,
            _requestContextMock.Object,
            clockMock.Object,
            _billingMock.Object,
            _billingCapacityMock.Object);
    }

    private static CreateAutomationRuleCommand Command() =>
        new(WorkspaceId, "My rule", "ItemAssigned", "Webhook",
            """{"webhookPath":"some-hook"}""");

    private void SetupCapability(bool isAvailable) =>
        _billingMock
            .Setup(b => b.GetCapabilityAsync(
                AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingCapabilityFact(
                IsAvailable: isAvailable, Limit: isAvailable ? 5 : null, Used: 0, Remaining: isAvailable ? 5 : null));

    [Fact]
    public async Task Handle_WhenCapabilityAvailable_CreatesRuleAndConsumesOneSlot()
    {
        SetupCapability(isAvailable: true);
        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _contextMock.Verify(c => c.AutomationRules.Add(It.IsAny<Domain.Automation.Rules.AutomationRule>()), Times.Once);
        _billingCapacityMock.Verify(
            c => c.ConsumeAsync(It.IsAny<ConsumeCapacityRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCapabilityUnavailable_FailsBeforeRuleMutationOrConsume()
    {
        SetupCapability(isAvailable: false);
        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        _contextMock.Verify(c => c.AutomationRules.Add(It.IsAny<Domain.Automation.Rules.AutomationRule>()), Times.Never);
        _billingCapacityMock.Verify(
            c => c.ConsumeAsync(It.IsAny<ConsumeCapacityRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBillingDependencyFails_FailsBeforeRuleMutationOrConsume()
    {
        _billingMock
            .Setup(b => b.GetCapabilityAsync(
                AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("billing unavailable"));
        var sut = CreateSut();

        await sut.Invoking(s => s.Handle(Command(), CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
        _contextMock.Verify(c => c.AutomationRules.Add(It.IsAny<Domain.Automation.Rules.AutomationRule>()), Times.Never);
        _billingCapacityMock.Verify(
            c => c.ConsumeAsync(It.IsAny<ConsumeCapacityRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_QueriesExactCapabilityAndAmount()
    {
        SetupCapability(isAvailable: true);
        var sut = CreateSut();

        await sut.Handle(Command(), CancellationToken.None);

        _billingMock.Verify(b => b.GetCapabilityAsync(
            AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ConsumesOneSlotBoundToCreatedRuleIdentity()
    {
        // TAC-BI-FLOW-03 wiring: the capacity consume is stable-idempotent for
        // the created rule — retrying the same logical create never
        // double-consumes because the logical operation id IS the rule identity.
        SetupCapability(isAvailable: true);
        ConsumeCapacityRequest? captured = null;
        _billingCapacityMock
            .Setup(c => c.ConsumeAsync(It.IsAny<ConsumeCapacityRequest>(), It.IsAny<CancellationToken>()))
            .Callback<ConsumeCapacityRequest, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(new ConsumeCapacityResult(AlreadyConsumed: false, Remaining: 4));
        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        var ruleId = result.Data;
        captured.Should().NotBeNull();
        captured!.Operation.LogicalOperationId.Should().Be(ruleId);
        captured.Operation.SourceResource.Should().Be(ruleId.ToString());
        captured.Operation.CapabilityCode.Should().Be(BillingCapabilityCode.AutomationRule);
        captured.Operation.Amount.Should().Be(1);
        captured.Operation.AccountId.Should().Be(AccountId);
        captured.Operation.WorkspaceId.Should().Be(WorkspaceId);
        captured.Operation.ActorUserId.Should().Be(UserId);
        captured.Operation.OccurredAt.Should().Be(TestNow);
    }
}
