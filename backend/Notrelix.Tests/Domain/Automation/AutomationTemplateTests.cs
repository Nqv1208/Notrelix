using FluentAssertions;
using Notrelix.Domain.Automation.Templates;
using Notrelix.Domain.Common;
using Xunit;

namespace Notrelix.Domain.Tests.Automation;

public class AutomationTemplateTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var definition = JsonValue.Create("{\"trigger\":\"item.created\"}");
        var template = AutomationTemplate.Create("Notify Team", "Notifications", definition, Guid.NewGuid(), DateTimeOffset.UtcNow);

        template.Name.Should().Be("Notify Team");
        template.Category.Should().Be("Notifications");
        template.Definition.Should().Be(definition);
        template.Status.Should().Be(AutomationTemplateStatus.Published);
        template.DomainEvents.Should().ContainSingle(e => e is AutomationTemplateCreatedDomainEvent);
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => AutomationTemplate.Create("", "Category", JsonValue.EmptyObject(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithNullDefinition_ShouldThrow()
    {
        var act = () => AutomationTemplate.Create("Name", "Category", null!, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }
}
