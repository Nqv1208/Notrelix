using FluentAssertions;
using Notrelix.Domain.Automation.RulesEngine;
using Notrelix.Domain.Common.Exceptions;
using Xunit;

namespace Notrelix.Domain.Tests.Automation;

public class AutomationTriggerValidatorTests
{
    [Fact]
    public void Validate_ScheduleTrigger_WithCron_ShouldNotThrow()
    {
        var trigger = AutomationTriggerDefinition.Create("ScheduleTrigger", "{\"cron\":\"0 9 * * *\"}");
        var act = () => AutomationTriggerValidator.Validate(trigger);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ScheduleTrigger_WithInterval_ShouldNotThrow()
    {
        var trigger = AutomationTriggerDefinition.Create("ScheduleTrigger", "{\"interval\":\"PT1H\"}");
        var act = () => AutomationTriggerValidator.Validate(trigger);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ScheduleTrigger_WithoutCronOrInterval_ShouldThrow()
    {
        var trigger = AutomationTriggerDefinition.Create("ScheduleTrigger", "{\"foo\":\"bar\"}");
        var act = () => AutomationTriggerValidator.Validate(trigger);
        act.Should().Throw<BusinessRuleException>().WithMessage("*'cron'*'interval'*");
    }

    [Fact]
    public void Validate_FieldChanged_WithFieldId_ShouldNotThrow()
    {
        var trigger = AutomationTriggerDefinition.Create("FieldChanged", "{\"fieldId\":\"status\"}");
        var act = () => AutomationTriggerValidator.Validate(trigger);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_FieldChanged_WithoutFieldId_ShouldThrow()
    {
        var trigger = AutomationTriggerDefinition.Create("FieldChanged", "{\"foo\":\"bar\"}");
        var act = () => AutomationTriggerValidator.Validate(trigger);
        act.Should().Throw<BusinessRuleException>().WithMessage("*'fieldId'*");
    }

    [Fact]
    public void Validate_ItemMovedToGroup_WithGroupId_ShouldNotThrow()
    {
        var trigger = AutomationTriggerDefinition.Create("ItemMovedToGroup", "{\"groupId\":\"g1\"}");
        var act = () => AutomationTriggerValidator.Validate(trigger);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ItemMovedToGroup_WithoutGroupIdOrFromGroupId_ShouldThrow()
    {
        var trigger = AutomationTriggerDefinition.Create("ItemMovedToGroup", "{\"foo\":\"bar\"}");
        var act = () => AutomationTriggerValidator.Validate(trigger);
        act.Should().Throw<BusinessRuleException>().WithMessage("*'groupId'*'fromGroupId'*");
    }

    [Fact]
    public void Validate_ItemCreated_WithoutConfig_ShouldNotThrow()
    {
        var trigger = AutomationTriggerDefinition.Create("ItemCreated");
        var act = () => AutomationTriggerValidator.Validate(trigger);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ItemUpdated_WithoutConfig_ShouldNotThrow()
    {
        var trigger = AutomationTriggerDefinition.Create("ItemUpdated");
        var act = () => AutomationTriggerValidator.Validate(trigger);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ItemDeleted_WithoutConfig_ShouldNotThrow()
    {
        var trigger = AutomationTriggerDefinition.Create("ItemDeleted");
        var act = () => AutomationTriggerValidator.Validate(trigger);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_FormSubmitted_WithoutConfig_ShouldNotThrow()
    {
        var trigger = AutomationTriggerDefinition.Create("FormSubmitted");
        var act = () => AutomationTriggerValidator.Validate(trigger);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ItemAssigned_WithoutConfig_ShouldNotThrow()
    {
        var trigger = AutomationTriggerDefinition.Create("ItemAssigned");
        var act = () => AutomationTriggerValidator.Validate(trigger);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_NullConfigOnScheduleTrigger_ShouldThrow()
    {
        var trigger = AutomationTriggerDefinition.Create("ScheduleTrigger");
        var act = () => AutomationTriggerValidator.Validate(trigger);
        act.Should().Throw<BusinessRuleException>().WithMessage("*configuration*");
    }

    [Fact]
    public void Validate_InvalidConfigJson_ShouldThrow()
    {
        var act = () => AutomationTriggerValidator.Validate(AutomationTriggerDefinition.Create("FieldChanged", "not-json"));
        act.Should().Throw<BusinessRuleException>().WithMessage("*JSON*");
    }
}
