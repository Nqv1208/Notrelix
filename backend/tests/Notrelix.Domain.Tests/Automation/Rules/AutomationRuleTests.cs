using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Domain.Tests.Automation;

[CoversAggregate(typeof(AutomationRule))]
public class AutomationRuleTests
{
    private static string? GetActionConfig(string actionType) => actionType switch
    {
        "Webhook" => """{"url":"https://example.com/webhook"}""",
        "SendEmail" => """{"templateId":"tpl_1"}""",
        "SlackMessage" => """{"channelId":"C123","text":"test"}""",
        _ => null
    };

    private static AutomationConfiguration CreateConfig(string triggerType = "ItemCreated", string actionType = "Webhook")
    {
        var trigger = AutomationTriggerDefinition.Create(triggerType);
        var action = AutomationActionDefinition.Create(actionType, GetActionConfig(actionType));
        return AutomationConfiguration.Create(trigger, action);
    }

    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var config = CreateConfig("ItemCreated", "Webhook");
        var rule = AutomationRule.Create(Guid.NewGuid(), workspaceId, "Notify on high priority", config, createdBy, DateTimeOffset.UtcNow);

        rule.Name.Should().Be("Notify on high priority");
        rule.Status.Should().Be(AutomationRuleStatus.Draft);
        rule.DomainEvents.Should().ContainSingle(e => e is AutomationRuleCreatedDomainEvent);
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Enable), MutationScenario.Event, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Enable_ShouldChangeStatus_AndRaiseEvent()
    {
        var createdBy = Guid.NewGuid();
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", CreateConfig(), createdBy, DateTimeOffset.UtcNow);
        ((IHasDomainEvents)rule).ClearDomainEvents();

        rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Status.Should().Be(AutomationRuleStatus.Active);
        rule.DomainEvents.Should().ContainSingle(e => e is AutomationRuleEnabledDomainEvent);
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Enable), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Enable_WhenAlreadyActive_ShouldBeNoOp()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", CreateConfig(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)rule).ClearDomainEvents();

        rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Enable), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Enable_WhenDeleted_ShouldThrow()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", CreateConfig(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Disable), MutationScenario.Event, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Disable_ShouldChangeStatus_AndRaiseEvent()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", CreateConfig(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)rule).ClearDomainEvents();

        rule.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Status.Should().Be(AutomationRuleStatus.Disabled);
        rule.DomainEvents.Should().ContainSingle(e => e is AutomationRuleDisabledDomainEvent);
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Disable), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Disable_WhenAlreadyDisabled_ShouldBeNoOp()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", CreateConfig(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)rule).ClearDomainEvents();

        rule.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Disable), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Disable_WhenDeleted_ShouldThrow()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", CreateConfig(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => rule.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.UpdateConfiguration), MutationScenario.Valid, typeof(AutomationConfiguration), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UpdateConfiguration_ShouldUpdate()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", CreateConfig(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var newConfig = CreateConfig("ItemUpdated", "SendEmail");

        rule.UpdateConfiguration(newConfig, Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Configuration.Trigger.Type.Should().Be("ItemUpdated");
        rule.Configuration.Action.Type.Should().Be("SendEmail");
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.UpdateConfiguration), MutationScenario.NoOp, typeof(AutomationConfiguration), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UpdateConfiguration_WhenSameConfig_ShouldBeNoOp()
    {
        var config = CreateConfig("ItemCreated", "Webhook");
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", config, Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)rule).ClearDomainEvents();

        rule.UpdateConfiguration(config, Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.UpdateConfiguration), MutationScenario.Invalid, typeof(AutomationConfiguration), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UpdateConfiguration_WhenDeleted_ShouldThrow()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", CreateConfig(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => rule.UpdateConfiguration(CreateConfig(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.UpdateConfiguration), MutationScenario.Event, typeof(AutomationConfiguration), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UpdateConfiguration_ShouldRaiseConfigurationChangedEvent()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", CreateConfig(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)rule).ClearDomainEvents();

        rule.UpdateConfiguration(CreateConfig("ItemUpdated", "SlackMessage"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.DomainEvents.Should().ContainSingle(e => e is AutomationConfigurationChangedDomainEvent);
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Delete), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_ShouldSetStatus_AndRaiseEvent()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", CreateConfig(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)rule).ClearDomainEvents();

        rule.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.IsDeleted.Should().BeTrue();
        rule.Status.Should().Be(AutomationRuleStatus.Disabled);
        rule.DomainEvents.Should().ContainSingle(e => e is AutomationRuleDeletedDomainEvent);
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Delete), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_WhenAlreadyDeleted_ShouldBeNoOp()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", CreateConfig(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)rule).ClearDomainEvents();

        rule.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.DomainEvents.Should().BeEmpty();
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_ShouldSetStatus_AndRaiseEvent()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", CreateConfig(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.Delete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)rule).ClearDomainEvents();

        rule.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.IsDeleted.Should().BeFalse();
        rule.Status.Should().Be(AutomationRuleStatus.Draft);
        rule.DomainEvents.Should().ContainSingle(e => e is AutomationRuleRestoredDomainEvent);
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Restore), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_WhenNotDeleted_ShouldBeNoOp()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", CreateConfig(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)rule).ClearDomainEvents();

        rule.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.DomainEvents.Should().BeEmpty();
    }
}
