using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Automation.Rules.Commands.CreateAutomationRule;
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
            _billingMock.Object);
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
    public async Task Handle_WhenCapabilityAvailable_CreatesRule()
    {
        SetupCapability(isAvailable: true);
        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _contextMock.Verify(c => c.AutomationRules.Add(It.IsAny<Domain.Automation.Rules.AutomationRule>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCapabilityUnavailable_FailsBeforeRuleMutation()
    {
        SetupCapability(isAvailable: false);
        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        _contextMock.Verify(c => c.AutomationRules.Add(It.IsAny<Domain.Automation.Rules.AutomationRule>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBillingDependencyFails_FailsBeforeRuleMutation()
    {
        _billingMock
            .Setup(b => b.GetCapabilityAsync(
                AccountId, WorkspaceId, BillingCapabilityCode.AutomationRule, 1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("billing unavailable"));
        var sut = CreateSut();

        await sut.Invoking(s => s.Handle(Command(), CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
        _contextMock.Verify(c => c.AutomationRules.Add(It.IsAny<Domain.Automation.Rules.AutomationRule>()), Times.Never);
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
}
