using FluentAssertions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Domain.Tests.Automation.Rules;

public class AutomationRuleLifecycleTests
{
    private static AutomationConfiguration CreateConfig(string triggerType = "ItemCreated", string actionType = "Webhook")
    {
        var trigger = AutomationTriggerDefinition.Create(triggerType);
        var action = AutomationActionDefinition.Create(actionType);
        return AutomationConfiguration.Create(trigger, action);
    }

    [Fact]
    public void Enable_DeletedRule_ShouldReject()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", CreateConfig(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void Disable_DeletedRule_ShouldReject()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", CreateConfig(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => rule.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void Enable_DeletedRule_ShouldNotChangeStatus()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", CreateConfig(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>();

        rule.Status.Should().Be(AutomationRuleStatus.Disabled);
    }

    [Fact]
    public void Restore_thenEnable_ShouldSucceed()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", CreateConfig(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Status.Should().Be(AutomationRuleStatus.Active);
    }

    [Fact]
    public void Enable_afterRestore_ShouldRaiseEnabledEvent()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", CreateConfig(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)rule).ClearDomainEvents();

        rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.DomainEvents.Should().ContainSingle(e => e is AutomationRuleEnabledDomainEvent);
    }

    [Fact]
    public void SoftDelete_ShouldSetStatusAndAudit()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", CreateConfig(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)rule).ClearDomainEvents();

        var deletedBy = Guid.NewGuid();
        var deletedAt = DateTimeOffset.UtcNow;
        rule.SoftDelete(deletedBy, deletedAt);

        rule.IsDeleted.Should().BeTrue();
        rule.Status.Should().Be(AutomationRuleStatus.Disabled);
        rule.UpdatedAt.Should().Be(deletedAt);
        rule.UpdatedBy.Should().Be(deletedBy);
        rule.Version.Should().Be(3);
    }

    [Fact]
    public void Restore_ShouldSetStatusAndAudit()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", CreateConfig(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        ((IHasDomainEvents)rule).ClearDomainEvents();

        var restoredBy = Guid.NewGuid();
        var restoredAt = DateTimeOffset.UtcNow;
        rule.Restore(restoredBy, restoredAt);

        rule.IsDeleted.Should().BeFalse();
        rule.Status.Should().Be(AutomationRuleStatus.Draft);
        rule.UpdatedAt.Should().Be(restoredAt);
        rule.UpdatedBy.Should().Be(restoredBy);
        rule.Version.Should().Be(3);
    }

    [Fact]
    public void Enable_thenDisable_ShouldBeCorrectVersion()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), Guid.NewGuid(), "Rule", CreateConfig(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.Version.Should().Be(1);

        rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.Version.Should().Be(2);

        rule.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.Version.Should().Be(3);
    }
}
