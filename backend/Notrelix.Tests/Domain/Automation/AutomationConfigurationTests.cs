using FluentAssertions;
using Notrelix.Domain.Automation.RulesEngine;
using Notrelix.Domain.Common.Exceptions;
using Xunit;

namespace Notrelix.Domain.Tests.Automation;

public class AutomationConfigurationTests
{
    private static readonly AutomationTriggerDefinition SampleTrigger = AutomationTriggerDefinition.Create("ItemCreated");
    private static readonly AutomationActionDefinition SampleAction = AutomationActionDefinition.Create("NotifyMember");

    [Fact]
    public void Create_ShouldSucceed()
    {
        var config = AutomationConfiguration.Create(SampleTrigger, SampleAction);

        config.Trigger.Should().Be(SampleTrigger);
        config.Action.Should().Be(SampleAction);
        config.Condition.Should().BeNull();
    }

    [Fact]
    public void Create_WithCondition_ShouldSetCondition()
    {
        var condition = AutomationConditionDefinition.Create("{\"field\":\"status\"}");

        var config = AutomationConfiguration.Create(SampleTrigger, SampleAction, condition);

        config.Condition.Should().Be(condition);
    }

    [Fact]
    public void Create_WithNullTrigger_ShouldThrow()
    {
        var act = () => AutomationConfiguration.Create(null!, SampleAction);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithNullAction_ShouldThrow()
    {
        var act = () => AutomationConfiguration.Create(SampleTrigger, null!);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var c1 = AutomationConfiguration.Create(SampleTrigger, SampleAction);
        var c2 = AutomationConfiguration.Create(SampleTrigger, SampleAction);

        c1.Should().Be(c2);
    }

    [Fact]
    public void Equality_DifferentTrigger_ShouldNotBeEqual()
    {
        var otherTrigger = AutomationTriggerDefinition.Create("FieldChanged");

        var c1 = AutomationConfiguration.Create(SampleTrigger, SampleAction);
        var c2 = AutomationConfiguration.Create(otherTrigger, SampleAction);

        c1.Should().NotBe(c2);
    }
}
