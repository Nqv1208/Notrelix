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
        var createdBy = Guid.NewGuid();
        var rule = AutomationRule.Create(workspaceId, "Notify on high priority", "item.created", "send_slack", createdBy, DateTimeOffset.UtcNow);

        rule.Name.Should().Be("Notify on high priority");
        rule.Status.Should().Be(AutomationRuleStatus.Draft);
        rule.DomainEvents.Should().ContainSingle(e => e is AutomationRuleCreatedEvent);
    }

    [Fact]
    public void Enable_ShouldChangeStatus_AndRaiseEvent()
    {
        var createdBy = Guid.NewGuid();
        var rule = AutomationRule.Create(Guid.NewGuid(), "Rule", "item.updated", "webhook", createdBy, DateTimeOffset.UtcNow);
        rule.ClearDomainEvents();

        rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Status.Should().Be(AutomationRuleStatus.Active);
        rule.DomainEvents.Should().ContainSingle(e => e is AutomationRuleEnabledEvent);
    }
}
