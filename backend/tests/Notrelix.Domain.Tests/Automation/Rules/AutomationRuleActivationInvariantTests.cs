using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Domain.Tests.Automation;

/// <summary>
/// Tests for the activation invariant:
/// Status == Active ⇒ Name valid ⇒ Configuration exists ⇒ Trigger valid ⇒ Action valid
/// </summary>
[CoversAggregate(typeof(AutomationRule))]
public class AutomationRuleActivationInvariantTests
{
    private static AutomationConfiguration CreateValidConfig()
    {
        var trigger = AutomationTriggerDefinition.Create("ItemCreated");
        var action = AutomationActionDefinition.Create("Webhook", """{"url":"https://example.com/webhook"}""");
        return AutomationConfiguration.Create(trigger, action);
    }

    private static AutomationRule CreateDraftRule()
    {
        return AutomationRule.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Test Rule",
            CreateValidConfig(), Guid.NewGuid(), DateTimeOffset.UtcNow);
    }

    // ── Enable validation ─────────────────────────────────────────────────

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Enable), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Enable_WhenTriggerDefinitionInvalid_ShouldReject()
    {
        // ScheduleTrigger requires cron or interval config
        var invalidTrigger = AutomationTriggerDefinition.Create("ScheduleTrigger");
        var validAction = AutomationActionDefinition.Create("Webhook", """{"url":"https://example.com"}""");
        var config = AutomationConfiguration.Create(invalidTrigger, validAction);
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", config, Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*ScheduleTrigger*");
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Enable), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Enable_WhenActionDefinitionInvalid_ShouldReject()
    {
        // Webhook action requires url config
        var validTrigger = AutomationTriggerDefinition.Create("ItemCreated");
        var invalidAction = AutomationActionDefinition.Create("Webhook");
        var config = AutomationConfiguration.Create(validTrigger, invalidAction);
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", config, Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*Webhook*");
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Enable), MutationScenario.FailureAtomicity, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Enable_WhenRejected_ShouldBeFailureAtomic()
    {
        var invalidTrigger = AutomationTriggerDefinition.Create("ScheduleTrigger");
        var validAction = AutomationActionDefinition.Create("Webhook", """{"url":"https://example.com"}""");
        var config = AutomationConfiguration.Create(invalidTrigger, validAction);
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", config, Guid.NewGuid(), DateTimeOffset.UtcNow);

        var statusBefore = rule.Status;
        var configBefore = rule.Configuration;
        var updatedAtBefore = rule.UpdatedAt;
        var updatedByBefore = rule.UpdatedBy;
        var versionBefore = rule.Version;
        var eventsBefore = rule.DomainEvents.Count;

        var act = () => rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();

        rule.Status.Should().Be(statusBefore);
        rule.Configuration.Should().Be(configBefore);
        rule.UpdatedAt.Should().Be(updatedAtBefore);
        rule.UpdatedBy.Should().Be(updatedByBefore);
        rule.Version.Should().Be(versionBefore);
        rule.DomainEvents.Count.Should().Be(eventsBefore);
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Enable), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Enable_WhenAlreadyActive_ShouldBeNoOp()
    {
        var rule = CreateDraftRule();
        rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)rule).ClearDomainEvents();
        var versionBefore = rule.Version;

        rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Version.Should().Be(versionBefore);
        rule.DomainEvents.Should().BeEmpty();
    }

    // ── UpdateConfiguration validation when Active ────────────────────────

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.UpdateConfiguration), MutationScenario.Invalid, typeof(AutomationConfiguration), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UpdateConfiguration_WhenActiveAndInvalidTrigger_ShouldReject()
    {
        var rule = CreateDraftRule();
        rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var invalidTrigger = AutomationTriggerDefinition.Create("ScheduleTrigger");
        var validAction = AutomationActionDefinition.Create("Webhook", """{"url":"https://example.com"}""");
        var invalidConfig = AutomationConfiguration.Create(invalidTrigger, validAction);

        var act = () => rule.UpdateConfiguration(invalidConfig, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*ScheduleTrigger*");
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.UpdateConfiguration), MutationScenario.Invalid, typeof(AutomationConfiguration), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UpdateConfiguration_WhenActiveAndInvalidAction_ShouldReject()
    {
        var rule = CreateDraftRule();
        rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var validTrigger = AutomationTriggerDefinition.Create("ItemCreated");
        var invalidAction = AutomationActionDefinition.Create("Webhook");
        var invalidConfig = AutomationConfiguration.Create(validTrigger, invalidAction);

        var act = () => rule.UpdateConfiguration(invalidConfig, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*Webhook*");
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.UpdateConfiguration), MutationScenario.FailureAtomicity, typeof(AutomationConfiguration), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UpdateConfiguration_WhenActiveAndRejected_ShouldBeFailureAtomic()
    {
        var rule = CreateDraftRule();
        rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var statusBefore = rule.Status;
        var configBefore = rule.Configuration;
        var updatedAtBefore = rule.UpdatedAt;
        var updatedByBefore = rule.UpdatedBy;
        var versionBefore = rule.Version;
        var eventsBefore = rule.DomainEvents.Count;

        var invalidTrigger = AutomationTriggerDefinition.Create("ScheduleTrigger");
        var validAction = AutomationActionDefinition.Create("Webhook", """{"url":"https://example.com"}""");
        var invalidConfig = AutomationConfiguration.Create(invalidTrigger, validAction);

        var act = () => rule.UpdateConfiguration(invalidConfig, Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();

        rule.Status.Should().Be(statusBefore);
        rule.Configuration.Should().Be(configBefore);
        rule.UpdatedAt.Should().Be(updatedAtBefore);
        rule.UpdatedBy.Should().Be(updatedByBefore);
        rule.Version.Should().Be(versionBefore);
        rule.DomainEvents.Count.Should().Be(eventsBefore);
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.UpdateConfiguration), MutationScenario.Valid, typeof(AutomationConfiguration), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UpdateConfiguration_WhenActiveAndValid_ShouldSucceed()
    {
        var rule = CreateDraftRule();
        rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)rule).ClearDomainEvents();

        var newTrigger = AutomationTriggerDefinition.Create("ItemUpdated");
        var newAction = AutomationActionDefinition.Create("SendEmail", """{"templateId":"tpl_1"}""");
        var newConfig = AutomationConfiguration.Create(newTrigger, newAction);

        rule.UpdateConfiguration(newConfig, Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Configuration.Trigger.Type.Should().Be("ItemUpdated");
        rule.Configuration.Action.Type.Should().Be("SendEmail");
        rule.DomainEvents.Should().ContainSingle(e => e is AutomationConfigurationChangedDomainEvent);
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.UpdateConfiguration), MutationScenario.Valid, typeof(AutomationConfiguration), typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UpdateConfiguration_WhenDraftAndInvalid_ShouldAllow()
    {
        // Draft rules can have invalid config - only Active requires validation
        var rule = CreateDraftRule();

        var invalidTrigger = AutomationTriggerDefinition.Create("ScheduleTrigger");
        var validAction = AutomationActionDefinition.Create("Webhook", """{"url":"https://example.com"}""");
        var invalidConfig = AutomationConfiguration.Create(invalidTrigger, validAction);

        var act = () => rule.UpdateConfiguration(invalidConfig, Guid.NewGuid(), DateTimeOffset.UtcNow);

        act.Should().NotThrow();
        rule.Configuration.Trigger.Type.Should().Be("ScheduleTrigger");
    }
}
