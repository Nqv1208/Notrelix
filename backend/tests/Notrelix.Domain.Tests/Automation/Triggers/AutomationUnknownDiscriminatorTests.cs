using FluentAssertions;
using Notrelix.Domain.Automation;
using Notrelix.Domain.Automation.Actions;
using Notrelix.Domain.Automation.Conditions;
using Notrelix.Domain.Automation.RulesEngine;
using Notrelix.Domain.Automation.Triggers;

namespace Notrelix.Domain.Tests.Automation;

public class AutomationUnknownDiscriminatorTests
{
    [Fact]
    public void TriggerDefinition_WithUnknownType_ShouldThrow()
    {
        var act = () => AutomationTriggerDefinition.Create("UnknownTrigger");
        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(AutomationRuleCodes.Automation_Trigger_InvalidType);
    }

    [Fact]
    public void TriggerDefinition_WithEmptyType_ShouldThrow()
    {
        var act = () => AutomationTriggerDefinition.Create("");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ActionDefinition_WithUnknownType_ShouldThrow()
    {
        var act = () => AutomationActionDefinition.Create("UnknownAction");
        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(AutomationRuleCodes.Automation_Action_InvalidType);
    }

    [Fact]
    public void ActionDefinition_WithEmptyType_ShouldThrow()
    {
        var act = () => AutomationActionDefinition.Create("");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void TriggerEntity_WithUnknownType_ShouldSucceed()
    {
        var trigger = AutomationTrigger.Create(Guid.NewGuid(), (AutomationTriggerType)999, TriggerConfig.Create(JsonValue.EmptyObject()));
        trigger.Should().NotBeNull();
    }

    [Fact]
    public void ActionEntity_WithUnknownType_ShouldSucceed()
    {
        var action = AutomationAction.Create(Guid.NewGuid(), (AutomationActionType)999, ActionConfig.Create(JsonValue.EmptyObject()), 0);
        action.Should().NotBeNull();
    }

    [Fact]
    public void ConditionEntity_WithUnknownType_ShouldSucceed()
    {
        var condition = AutomationCondition.Create(Guid.NewGuid(), (AutomationConditionType)999, ConditionConfig.Create(JsonValue.EmptyObject()), 0);
        condition.Should().NotBeNull();
    }

    [Fact]
    public void TriggerConfig_WithInvalidJson_ShouldThrow()
    {
        var act = () => AutomationTriggerDefinition.Create("ScheduleTrigger", "not-json");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void ActionConfig_NullJson_ShouldSucceed()
    {
        var def = AutomationActionDefinition.Create("NotifyMember", null);
        def.Configuration.Should().BeNull();
    }

    [Fact]
    public void TriggerConfig_NullJson_ShouldSucceed()
    {
        var def = AutomationTriggerDefinition.Create("ItemCreated", null);
        def.Configuration.Should().BeNull();
    }
}
