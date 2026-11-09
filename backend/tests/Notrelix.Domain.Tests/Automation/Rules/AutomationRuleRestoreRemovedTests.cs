using FluentAssertions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Domain.Tests.Automation.Rules;

/// <summary>
/// UC-AUT-A07: a removed (deleted) rule is restored as Disabled, never auto-enabled, whatever its status was.
/// </summary>
public class AutomationRuleRestoreRemovedTests
{
    private static AutomationRule CreateEnabledRule()
    {
        var rule = AutomationRule.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Rule",
            AutomationConfiguration.Create(
                AutomationTriggerDefinition.Create("ItemCreated"),
                AutomationActionDefinition.Create("Webhook", """{"webhookPath":"example/hook"}""")),
            Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        return rule;
    }

    [Fact]
    public void RestoreRemoved_OfEnabledRule_ComesBackDisabled()
    {
        var rule = CreateEnabledRule();
        rule.Remove(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.RestoreRemoved(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Status.Should().Be(AutomationRuleStatus.Disabled);
        rule.IsRemoved.Should().BeFalse();
        rule.IsEnabled.Should().BeFalse();
        rule.DisabledReason.Should().BeNull();
    }

    [Fact]
    public void RestoreRemoved_OfDraftRule_ComesBackDisabled()
    {
        var rule = AutomationRule.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Draft",
            AutomationConfiguration.Create(
                AutomationTriggerDefinition.Create("ItemCreated"),
                AutomationActionDefinition.Create("Webhook", """{"webhookPath":"example/hook"}""")),
            Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.Remove(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.RestoreRemoved(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Status.Should().Be(AutomationRuleStatus.Disabled);
    }

    [Fact]
    public void RestoreRemoved_OfLiveRule_IsNoOp()
    {
        var rule = CreateEnabledRule();
        ((IHasDomainEvents)rule).ClearDomainEvents();
        var versionBefore = rule.Version;

        rule.RestoreRemoved(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Status.Should().Be(AutomationRuleStatus.Active);
        rule.Version.Should().Be(versionBefore);
        rule.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RestoreRemoved_IncrementsVersionOnce_AndRaisesRestoredEventOnly()
    {
        var rule = CreateEnabledRule();
        rule.Remove(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var versionBefore = rule.Version;
        ((IHasDomainEvents)rule).ClearDomainEvents();

        rule.RestoreRemoved(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Version.Should().Be(versionBefore + 1);
        rule.DomainEvents.Should().ContainSingle(e => e is AutomationRuleRestoredDomainEvent);
    }

    [Fact]
    public void RestoreRemoved_KeepsConfigurationAndName()
    {
        var rule = CreateEnabledRule();
        var name = rule.Name;
        var configuration = rule.Configuration;
        rule.Remove(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.RestoreRemoved(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Name.Should().Be(name);
        rule.Configuration.Should().Be(configuration);
    }
}
