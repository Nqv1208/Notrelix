using FluentAssertions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;
using Notrelix.Domain.Common.Exceptions;
using Xunit;

namespace Notrelix.Domain.Tests.Automation;

public class AutomationRuleValidatorTests
{
    [Fact]
    public void Validate_WithValidRule_ShouldNotThrow()
    {
        var action = AutomationActionDefinition.Create("SendEmail", """{"templateId":"tpl_1"}""");
        var config = AutomationConfiguration.Create(
            AutomationTriggerDefinition.Create("ItemCreated"),
            action);
        var rule = AutomationRule.Create(Guid.NewGuid(), "Valid Rule", config, Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => AutomationRuleValidator.Validate(rule);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithEmptyName_ShouldThrow()
    {
        var config = AutomationConfiguration.Create(
            AutomationTriggerDefinition.Create("ItemCreated"),
            AutomationActionDefinition.Create("SendEmail", """{"templateId":"tpl_1"}"""));
        var rule = AutomationRule.Create(Guid.NewGuid(), "Valid", config, Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.GetType().GetProperty("Name")!.SetValue(rule, "");

        var act = () => AutomationRuleValidator.Validate(rule);
        act.Should().Throw<BusinessRuleException>().WithMessage("*name cannot be empty*");
    }
}
