using FluentAssertions;
using Notrelix.Domain.Automation.RulesEngine;
using Xunit;

namespace Notrelix.Domain.Tests.Automation.Configuration;

public class AutomationConfigImmutabilityTests
{
    [Fact]
    public void AutomationConfiguration_Properties_AreImmutable()
    {
        var trigger = AutomationTriggerDefinition.Create("FieldChanged");
        var action = AutomationActionDefinition.Create("SendEmail");
        var condition = AutomationConditionDefinition.Create("{\"field\":\"status\"}");
        var cfg = AutomationConfiguration.Create(trigger, action, condition);

        cfg.Trigger.Should().Be(trigger);
        cfg.Action.Should().Be(action);
        cfg.Condition.Should().Be(condition);
        cfg.SchemaVersion.Should().Be(1);
    }

    [Fact]
    public void AutomationTriggerDefinition_Properties_AreImmutable()
    {
        var def = AutomationTriggerDefinition.Create("ItemCreated", "{\"field\":\"status\"}");
        def.Type.Should().Be("ItemCreated");
        def.Configuration.Should().Be("{\"field\":\"status\"}");
        def.SchemaVersion.Should().Be(1);
    }

    [Fact]
    public void AutomationActionDefinition_Properties_AreImmutable()
    {
        var def = AutomationActionDefinition.Create("CreateItem", "{\"field\":\"name\"}");
        def.Type.Should().Be("CreateItem");
        def.Configuration.Should().Be("{\"field\":\"name\"}");
        def.SchemaVersion.Should().Be(1);
    }

    [Fact]
    public void AutomationConditionDefinition_Properties_AreImmutable()
    {
        var def = AutomationConditionDefinition.Create("{\"field\":\"status\"}");
        def.SchemaVersion.Should().Be(1);
    }

    [Fact]
    public void AutomationConfiguration_WithNullCondition_ShouldWork()
    {
        var trigger = AutomationTriggerDefinition.Create("FieldChanged");
        var action = AutomationActionDefinition.Create("SendEmail");
        var cfg = AutomationConfiguration.Create(trigger, action);
        cfg.Condition.Should().BeNull();
    }

    [Fact]
    public void AutomationConfiguration_WithCondition_ShouldBeEqual()
    {
        var trigger = AutomationTriggerDefinition.Create("FieldChanged");
        var action = AutomationActionDefinition.Create("SendEmail");
        var condition = AutomationConditionDefinition.Create("{\"field\":\"status\"}");
        var a = AutomationConfiguration.Create(trigger, action, condition);
        var b = AutomationConfiguration.Create(trigger, action, condition);
        a.Should().Be(b);
    }

    [Fact]
    public void AutomationConfiguration_WithoutCondition_ShouldNotEqualWithCondition()
    {
        var trigger = AutomationTriggerDefinition.Create("FieldChanged");
        var action = AutomationActionDefinition.Create("SendEmail");
        var condition = AutomationConditionDefinition.Create("{\"field\":\"status\"}");
        var withCondition = AutomationConfiguration.Create(trigger, action, condition);
        var withoutCondition = AutomationConfiguration.Create(trigger, action);
        withCondition.Should().NotBe(withoutCondition);
    }
}
