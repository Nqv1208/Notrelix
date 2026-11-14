using Notrelix.Application.Features.Automation.Ports.WorkManagement;
using Notrelix.Application.Features.Automation.Rules.Commands.CreateAutomationRule;
using Notrelix.Application.Features.Automation.Rules.Services;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Tests.Features.Billing;

namespace Notrelix.Application.Tests.Features.Automation.Rules;

/// <summary>
/// Only enabled rules count toward the plan limit (owner decision 44, UC-AUT-M04): creating a Draft
/// takes no slot and is never refused by the rule limit; the slot is consumed on enable.
/// </summary>
public class CreateAutomationRuleDraftCapacityTests
{
    private static readonly DateTimeOffset TestNow = new(2025, 1, 15, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    private readonly Mock<IAutomationDbContext> _contextMock = new();
    private readonly Mock<ICurrentRequestContext> _requestContextMock = new();

    public CreateAutomationRuleDraftCapacityTests()
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
            new AutomationExecutorAuthority(new Mock<IWorkActionPort>().Object),
            new Mock<IWorkActionPort>().Object);
    }

    private static CreateAutomationRuleCommand Command() =>
        new(WorkspaceId, "My rule", "ItemAssigned", "Webhook",
            """{"webhookPath":"some-hook"}""");

    [Fact]
    public async Task Handle_CreatesDraft_WithoutAnyPlanSlotCheckOrConsume()
    {
        var sut = CreateSut();

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _contextMock.Verify(c => c.AutomationRules.Add(It.IsAny<Domain.Automation.Rules.AutomationRule>()), Times.Once);
    }
}
