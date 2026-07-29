using FluentAssertions;
using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Domain.Tests.Automation.Configuration;

public class AutomationDefinitionSchemaTests
{
    [Fact]
    public void AutomationConfiguration_ShouldHaveSchemaVersion1()
    {
        var cfg = AutomationConfiguration.Create(
            AutomationTriggerDefinition.Create("FieldChanged"),
            AutomationActionDefinition.Create("SendEmail"));
        cfg.SchemaVersion.Should().Be(1);
    }

    [Fact]
    public void AutomationTriggerDefinition_ShouldHaveSchemaVersion1()
    {
        var def = AutomationTriggerDefinition.Create("ItemCreated");
        def.SchemaVersion.Should().Be(1);
    }

    [Fact]
    public void AutomationActionDefinition_ShouldHaveSchemaVersion1()
    {
        var def = AutomationActionDefinition.Create("CreateItem");
        def.SchemaVersion.Should().Be(1);
    }

    [Fact]
    public void AutomationConditionDefinition_ShouldHaveSchemaVersion1()
    {
        var def = AutomationConditionDefinition.Create("{\"field\":\"status\"}");
        def.SchemaVersion.Should().Be(1);
    }

    [Fact]
    public void AutomationTriggerDefinition_Equality_BasedOnTypeAndConfig()
    {
        var a = AutomationTriggerDefinition.Create("FieldChanged", "{\"field\":\"status\"}");
        var b = AutomationTriggerDefinition.Create("FieldChanged", "{\"field\":\"status\"}");
        var c = AutomationTriggerDefinition.Create("FieldChanged", "{\"field\":\"assignee\"}");
        a.Should().Be(b);
        a.Should().NotBe(c);
    }

    [Fact]
    public void AutomationActionDefinition_Equality_BasedOnTypeAndConfig()
    {
        var a = AutomationActionDefinition.Create("SendEmail", "{\"to\":\"user\"}");
        var b = AutomationActionDefinition.Create("SendEmail", "{\"to\":\"user\"}");
        var c = AutomationActionDefinition.Create("SendEmail", "{\"to\":\"admin\"}");
        a.Should().Be(b);
        a.Should().NotBe(c);
    }

    [Fact]
    public void AutomationConfiguration_Equality_BasedOnTriggerActionCondition()
    {
        var trigger = AutomationTriggerDefinition.Create("FieldChanged");
        var action = AutomationActionDefinition.Create("SendEmail");
        var a = AutomationConfiguration.Create(trigger, action);
        var b = AutomationConfiguration.Create(trigger, action);
        var c = AutomationConfiguration.Create(
            AutomationTriggerDefinition.Create("ItemCreated"), action);
        a.Should().Be(b);
        a.Should().NotBe(c);
    }

    [Fact]
    public void AutomationConditionDefinition_NullJson_ShouldThrow()
    {
        var act = () => AutomationConditionDefinition.Create(null!);
        act.Should().Throw<BusinessRuleException>();
    }
}
