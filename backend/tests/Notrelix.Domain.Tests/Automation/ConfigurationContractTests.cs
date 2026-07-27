using FluentAssertions;
using Notrelix.Domain.Automation.RulesEngine;
using Notrelix.Domain.Automation.Scheduled;

namespace Notrelix.Domain.Tests.Automation;

public class ConfigurationContractTests
{
    [Fact]
    public void TriggerDefinition_ShouldHaveSchemaVersion()
    {
        var trigger = AutomationTriggerDefinition.Create("FieldChanged");

        trigger.SchemaVersion.Should().Be(1);
    }

    [Fact]
    public void TriggerDefinition_SchemaVersion_ShouldBeInEquality()
    {
        var t1 = AutomationTriggerDefinition.Create("FieldChanged");
        var t2 = AutomationTriggerDefinition.Create("FieldChanged");

        t1.Should().Be(t2);
        t1.GetHashCode().Should().Be(t2.GetHashCode());
    }

    [Fact]
    public void TriggerDefinition_InvalidType_ShouldThrow()
    {
        var act = () => AutomationTriggerDefinition.Create("NonexistentTrigger");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void TriggerDefinition_NullConfig_ShouldThrow()
    {
        var act = () => AutomationTriggerDefinition.Create("FieldChanged", "null");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void TriggerDefinition_InvalidJsonConfig_ShouldThrow()
    {
        var act = () => AutomationTriggerDefinition.Create("FieldChanged", "{bad}");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ActionDefinition_ShouldHaveSchemaVersion()
    {
        var action = AutomationActionDefinition.Create("Webhook");

        action.SchemaVersion.Should().Be(1);
    }

    [Fact]
    public void ActionDefinition_SchemaVersion_ShouldBeInEquality()
    {
        var a1 = AutomationActionDefinition.Create("Webhook");
        var a2 = AutomationActionDefinition.Create("Webhook");

        a1.Should().Be(a2);
    }

    [Fact]
    public void ActionDefinition_InvalidType_ShouldThrow()
    {
        var act = () => AutomationActionDefinition.Create("NonexistentAction");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ActionDefinition_NullConfig_ShouldThrow()
    {
        var act = () => AutomationActionDefinition.Create("Webhook", "null");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ConditionDefinition_ShouldHaveSchemaVersion()
    {
        var condition = AutomationConditionDefinition.Create("{\"op\":\"equals\"}");

        condition.SchemaVersion.Should().Be(1);
    }

    [Fact]
    public void ConditionDefinition_SchemaVersion_ShouldBeInEquality()
    {
        var c1 = AutomationConditionDefinition.Create("{\"op\":\"equals\"}");
        var c2 = AutomationConditionDefinition.Create("{\"op\":\"equals\"}");

        c1.Should().Be(c2);
    }

    [Fact]
    public void ConditionDefinition_NonObjectJson_ShouldThrow()
    {
        var act = () => AutomationConditionDefinition.Create("\"just a string\"");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ConditionDefinition_EmptyJson_ShouldThrow()
    {
        var act = () => AutomationConditionDefinition.Create("");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ScheduleDefinition_ShouldHaveSchemaVersion()
    {
        var schedule = ScheduleDefinition.Create("0 * * * *");

        schedule.SchemaVersion.Should().Be(1);
    }

    [Fact]
    public void ScheduleDefinition_SchemaVersion_ShouldBeInEquality()
    {
        var s1 = ScheduleDefinition.Create("0 * * * *");
        var s2 = ScheduleDefinition.Create("0 * * * *");

        s1.Should().Be(s2);
    }

    [Fact]
    public void ScheduleDefinition_EmptyCron_ShouldThrow()
    {
        var act = () => ScheduleDefinition.Create("");

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Configuration_ShouldHaveSchemaVersion()
    {
        var trigger = AutomationTriggerDefinition.Create("ItemCreated");
        var action = AutomationActionDefinition.Create("Webhook");
        var config = AutomationConfiguration.Create(trigger, action);

        config.SchemaVersion.Should().Be(1);
    }

    [Fact]
    public void Configuration_SchemaVersion_ShouldBeInEquality()
    {
        var trigger = AutomationTriggerDefinition.Create("ItemCreated");
        var action = AutomationActionDefinition.Create("Webhook");
        var c1 = AutomationConfiguration.Create(trigger, action);
        var c2 = AutomationConfiguration.Create(trigger, action);

        c1.Should().Be(c2);
    }

    [Fact]
    public void Configuration_NullTrigger_ShouldThrow()
    {
        var action = AutomationActionDefinition.Create("Webhook");

        var act = () => AutomationConfiguration.Create(null!, action);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Configuration_NullAction_ShouldThrow()
    {
        var trigger = AutomationTriggerDefinition.Create("ItemCreated");

        var act = () => AutomationConfiguration.Create(trigger, null!);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Configuration_WithCondition_ShouldPreserveCondition()
    {
        var trigger = AutomationTriggerDefinition.Create("FieldChanged");
        var action = AutomationActionDefinition.Create("UpdateField");
        var condition = AutomationConditionDefinition.Create("{\"field\":\"status\"}");
        var config = AutomationConfiguration.Create(trigger, action, condition);

        config.Condition.Should().NotBeNull();
        config.Condition!.RawJson.Should().Be("{\"field\":\"status\"}");
    }

    [Fact]
    public void Configuration_WithoutCondition_ShouldBeNull()
    {
        var trigger = AutomationTriggerDefinition.Create("ItemCreated");
        var action = AutomationActionDefinition.Create("Webhook");
        var config = AutomationConfiguration.Create(trigger, action);

        config.Condition.Should().BeNull();
    }
}
