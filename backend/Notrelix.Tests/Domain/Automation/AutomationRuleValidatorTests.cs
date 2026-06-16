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
        var rule = AutomationRule.Create(Guid.NewGuid(), "Valid Rule", "item.created", "send_email", Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => AutomationRuleValidator.Validate(rule);
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WithEmptyName_ShouldThrow()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), "Valid", "trigger", "action", Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.GetType().GetProperty("Name")!.SetValue(rule, "");

        var act = () => AutomationRuleValidator.Validate(rule);
        act.Should().Throw<BusinessRuleException>().WithMessage("*name cannot be empty*");
    }
}
