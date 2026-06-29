using FluentAssertions;
using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Domain.Tests.Automation;

public class AutomationTriggerDefinitionTests
{
    [Fact]
    public void Create_WithValidType_ShouldSucceed()
    {
        var trigger = AutomationTriggerDefinition.Create("FieldChanged");

        trigger.Type.Should().Be("FieldChanged");
        trigger.Configuration.Should().BeNull();
    }

    [Fact]
    public void Create_WithAllValidTypes_ShouldSucceed()
    {
        var validTypes = new[] { "FieldChanged", "ItemCreated", "ItemUpdated", "ItemDeleted", "ItemMovedToGroup", "FormSubmitted", "ScheduleTrigger" };
        foreach (var type in validTypes)
        {
            var trigger = AutomationTriggerDefinition.Create(type);
            trigger.Type.Should().Be(type);
        }
    }

    [Fact]
    public void Create_WithInvalidType_ShouldThrow()
    {
        var act = () => AutomationTriggerDefinition.Create("InvalidTrigger");
        act.Should().Throw<BusinessRuleException>().WithMessage("*Invalid trigger type*");
    }

    [Fact]
    public void Create_WithCaseInsensitiveType_ShouldSucceed()
    {
        var trigger = AutomationTriggerDefinition.Create("fieldchanged");

        trigger.Type.Should().Be("fieldchanged");
    }

    [Fact]
    public void Create_WithValidConfiguration_ShouldSucceed()
    {
        var trigger = AutomationTriggerDefinition.Create("FieldChanged", "{\"field\":\"status\"}");

        trigger.Configuration.Should().Be("{\"field\":\"status\"}");
    }

    [Fact]
    public void Create_WithInvalidConfigurationJson_ShouldThrow()
    {
        var act = () => AutomationTriggerDefinition.Create("FieldChanged", "{bad}");
        act.Should().Throw<BusinessRuleException>().WithMessage("*JSON*");
    }

    [Fact]
    public void Create_WithEmptyType_ShouldThrow()
    {
        var act = () => AutomationTriggerDefinition.Create("");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var t1 = AutomationTriggerDefinition.Create("ItemCreated");
        var t2 = AutomationTriggerDefinition.Create("ItemCreated");

        t1.Should().Be(t2);
    }
}
