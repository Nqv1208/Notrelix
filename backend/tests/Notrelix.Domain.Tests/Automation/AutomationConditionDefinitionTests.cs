using FluentAssertions;
using Notrelix.Domain.Automation.RulesEngine;
using Notrelix.Domain.Common.Exceptions;
using Xunit;

namespace Notrelix.Domain.Tests.Automation;

public class AutomationConditionDefinitionTests
{
    [Fact]
    public void Create_WithValidObjectJson_ShouldSucceed()
    {
        var condition = AutomationConditionDefinition.Create("{\"field\":\"status\",\"value\":\"done\"}");

        condition.RawJson.Should().Be("{\"field\":\"status\",\"value\":\"done\"}");
    }

    [Fact]
    public void Create_WithEmptyJson_ShouldThrow()
    {
        var act = () => AutomationConditionDefinition.Create("");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithInvalidJson_ShouldThrow()
    {
        var act = () => AutomationConditionDefinition.Create("{bad}");
        act.Should().Throw<BusinessRuleException>().WithMessage("*JSON*");
    }

    [Fact]
    public void Create_WithNonObjectJson_ShouldThrow()
    {
        var actArray = () => AutomationConditionDefinition.Create("[1,2,3]");
        actArray.Should().Throw<BusinessRuleException>().WithMessage("*object*");

        var actString = () => AutomationConditionDefinition.Create("\"string\"");
        actString.Should().Throw<BusinessRuleException>().WithMessage("*object*");

        var actNumber = () => AutomationConditionDefinition.Create("42");
        actNumber.Should().Throw<BusinessRuleException>().WithMessage("*object*");
    }

    [Fact]
    public void Equality_SameJson_ShouldBeEqual()
    {
        var c1 = AutomationConditionDefinition.Create("{\"x\":1}");
        var c2 = AutomationConditionDefinition.Create("{\"x\":1}");

        c1.Should().Be(c2);
    }
}
