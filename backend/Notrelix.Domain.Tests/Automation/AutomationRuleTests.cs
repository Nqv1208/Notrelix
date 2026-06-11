using FluentAssertions;
using Notrelix.Domain.Automation.Rules;
using Xunit;

namespace Notrelix.Domain.Tests.Automation;

public class AutomationRuleTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var rule = AutomationRule.Create(workspaceId, "Notify on high priority", Guid.NewGuid());

        rule.Name.Should().Be("Notify on high priority");
        rule.Status.Should().Be(AutomationRuleStatus.Draft);
        rule.DomainEvents.Should().ContainSingle(e => e is AutomationRuleCreatedEvent);
    }

    [Fact]
    public void Enable_ShouldChangeStatus_AndRaiseEvent()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), "Rule", Guid.NewGuid());
        rule.ClearDomainEvents();

        rule.Enable(Guid.NewGuid());

        rule.Status.Should().Be(AutomationRuleStatus.Active);
        rule.DomainEvents.Should().ContainSingle(e => e is AutomationRuleEnabledEvent);
    }
}
