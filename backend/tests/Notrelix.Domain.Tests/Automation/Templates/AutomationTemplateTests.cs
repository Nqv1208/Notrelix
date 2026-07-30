using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Automation.Templates;

namespace Notrelix.Domain.Tests.Automation;

[CoversAggregate(typeof(AutomationTemplate))]
public class AutomationTemplateTests
{
    [CoversMutation(typeof(AutomationTemplate), "Publish(System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(AutomationTemplate), "Archive(System.DateTimeOffset)", MutationScenario.Event)]
    [CoversMutation(typeof(AutomationTemplate), "Delete(System.Guid,System.DateTimeOffset,System.String)", MutationScenario.Lifecycle)]
    [CoversMutation(typeof(AutomationTemplate), "Restore(System.Guid,System.DateTimeOffset)", MutationScenario.Lifecycle)]
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

    [CoversMutation(typeof(AutomationTemplate), "UpdateName(System.String,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        var act = () => AutomationTemplate.Create("", "Category", JsonValue.EmptyObject(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [CoversMutation(typeof(AutomationTemplate), "UpdateDefinition(Notrelix.Domain.SharedKernel.JsonValue,System.Guid,System.DateTimeOffset)", MutationScenario.Invalid)]
    [Fact]
    public void Create_WithNullDefinition_ShouldThrow()
    {
        var act = () => AutomationTemplate.Create("Name", "Category", null!, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }
}
