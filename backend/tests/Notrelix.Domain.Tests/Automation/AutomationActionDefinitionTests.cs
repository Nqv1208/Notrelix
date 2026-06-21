using FluentAssertions;
using Notrelix.Domain.Automation.RulesEngine;
using Notrelix.Domain.Common.Exceptions;
using Xunit;

namespace Notrelix.Domain.Tests.Automation;

public class AutomationActionDefinitionTests
{
    [Fact]
    public void Create_WithValidType_ShouldSucceed()
    {
        var action = AutomationActionDefinition.Create("SendEmail");

        action.Type.Should().Be("SendEmail");
    }

    [Fact]
    public void Create_WithAllValidTypes_ShouldSucceed()
    {
        var validTypes = new[] { "SendEmail", "UpdateField", "CreateItem", "MoveItem", "NotifyMember", "Webhook", "SlackMessage" };
        foreach (var type in validTypes)
        {
            var action = AutomationActionDefinition.Create(type);
            action.Type.Should().Be(type);
        }
    }

    [Fact]
    public void Create_WithInvalidType_ShouldThrow()
    {
        var act = () => AutomationActionDefinition.Create("InvalidAction");
        act.Should().Throw<BusinessRuleException>().WithMessage("*Invalid action type*");
    }

    [Fact]
    public void Create_WithValidConfiguration_ShouldSucceed()
    {
        var action = AutomationActionDefinition.Create("Webhook", "{\"url\":\"https://example.com\"}");

        action.Configuration.Should().Be("{\"url\":\"https://example.com\"}");
    }

    [Fact]
    public void Create_WithInvalidConfiguration_ShouldThrow()
    {
        var act = () => AutomationActionDefinition.Create("Webhook", "not-json");
        act.Should().Throw<BusinessRuleException>().WithMessage("*JSON*");
    }

    [Fact]
    public void Create_WithEmptyType_ShouldThrow()
    {
        var act = () => AutomationActionDefinition.Create("");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        var a1 = AutomationActionDefinition.Create("CreateItem");
        var a2 = AutomationActionDefinition.Create("CreateItem");

        a1.Should().Be(a2);
    }
}
