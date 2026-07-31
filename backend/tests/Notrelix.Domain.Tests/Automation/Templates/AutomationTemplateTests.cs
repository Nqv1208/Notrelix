using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Automation.Templates;

namespace Notrelix.Domain.Tests.Automation;

[CoversAggregate(typeof(AutomationTemplate))]
public class AutomationTemplateTests
{
    [CoversMutation(typeof(AutomationTemplate), nameof(AutomationTemplate.Publish), MutationScenario.Event, typeof(DateTimeOffset))]
    [CoversMutation(typeof(AutomationTemplate), nameof(AutomationTemplate.Archive), MutationScenario.Event, typeof(DateTimeOffset))]
    [CoversMutation(typeof(AutomationTemplate), nameof(AutomationTemplate.Delete), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [CoversMutation(typeof(AutomationTemplate), nameof(AutomationTemplate.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
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

    [CoversMutation(typeof(AutomationTemplate), nameof(AutomationTemplate.UpdateName), MutationScenario.Invalid, typeof(string), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => AutomationTemplate.Create("", "Category", JsonValue.EmptyObject(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(AutomationTemplate), nameof(AutomationTemplate.UpdateDefinition), MutationScenario.Invalid, typeof(JsonValue), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Create_WithNullDefinition_ShouldThrow()
    {
        var act = () => AutomationTemplate.Create("Name", "Category", null!, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }
}
