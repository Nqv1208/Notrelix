using FluentAssertions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;
using Xunit;

namespace Notrelix.Domain.Tests.Automation;

public class AutomationRuleTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var workspaceId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var rule = AutomationRule.Create(workspaceId, "Notify on high priority", "item.created", "send_slack", createdBy, DateTimeOffset.UtcNow);

        rule.Name.Should().Be("Notify on high priority");
        rule.Status.Should().Be(AutomationRuleStatus.Draft);
        rule.DomainEvents.Should().ContainSingle(e => e is AutomationRuleCreatedEvent);
    }

    [Fact]
    public void Enable_ShouldChangeStatus_AndRaiseEvent()
    {
        var createdBy = Guid.NewGuid();
        var rule = AutomationRule.Create(Guid.NewGuid(), "Rule", "item.updated", "webhook", createdBy, DateTimeOffset.UtcNow);
        rule.ClearDomainEvents();

        rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Status.Should().Be(AutomationRuleStatus.Active);
        rule.DomainEvents.Should().ContainSingle(e => e is AutomationRuleEnabledEvent);
    }

    [Fact]
    public void Enable_WhenAlreadyActive_ShouldBeNoOp()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), "Rule", "trigger", "action", Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.ClearDomainEvents();

        rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Enable_WhenDeleted_ShouldThrow()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), "Rule", "trigger", "action", Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void Disable_ShouldChangeStatus_AndRaiseEvent()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), "Rule", "trigger", "action", Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.Enable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.ClearDomainEvents();

        rule.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.Status.Should().Be(AutomationRuleStatus.Disabled);
        rule.DomainEvents.Should().ContainSingle(e => e is AutomationRuleDisabledEvent);
    }

    [Fact]
    public void Disable_WhenAlreadyDisabled_ShouldBeNoOp()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), "Rule", "trigger", "action", Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.ClearDomainEvents();

        rule.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Disable_WhenDeleted_ShouldBeNoOp()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), "Rule", "trigger", "action", Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.ClearDomainEvents();

        rule.Disable(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateConfiguration_ShouldUpdate()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), "Rule", "trigger", "action", Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.UpdateConfiguration("{\"key\":\"value\"}");

        rule.Configuration.Should().Be("{\"key\":\"value\"}");
    }

    [Fact]
    public void UpdateConfiguration_WhenSameConfig_ShouldBeNoOp()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), "Rule", "trigger", "action", Guid.NewGuid(), DateTimeOffset.UtcNow, configuration: "config");

        rule.UpdateConfiguration("config");

        rule.Configuration.Should().Be("config");
    }

    [Fact]
    public void UpdateConfiguration_WhenDeleted_ShouldThrow()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), "Rule", "trigger", "action", Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = () => rule.UpdateConfiguration("new");
        act.Should().Throw<DomainException>().WithMessage("*deleted*");
    }

    [Fact]
    public void SoftDelete_ShouldSetStatus_AndRaiseEvent()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), "Rule", "trigger", "action", Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.ClearDomainEvents();

        rule.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.IsDeleted.Should().BeTrue();
        rule.Status.Should().Be(AutomationRuleStatus.Disabled);
        rule.DomainEvents.Should().ContainSingle(e => e is AutomationRuleDeletedEvent);
    }

    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_ShouldBeNoOp()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), "Rule", "trigger", "action", Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.ClearDomainEvents();

        rule.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Restore_ShouldSetStatus_AndRaiseEvent()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), "Rule", "trigger", "action", Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.ClearDomainEvents();

        rule.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.IsDeleted.Should().BeFalse();
        rule.Status.Should().Be(AutomationRuleStatus.Draft);
        rule.DomainEvents.Should().ContainSingle(e => e is AutomationRuleRestoredEvent);
    }

    [Fact]
    public void Restore_WhenNotDeleted_ShouldBeNoOp()
    {
        var rule = AutomationRule.Create(Guid.NewGuid(), "Rule", "trigger", "action", Guid.NewGuid(), DateTimeOffset.UtcNow);
        rule.ClearDomainEvents();

        rule.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        rule.DomainEvents.Should().BeEmpty();
    }
}
