using FluentAssertions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;
using Notrelix.Domain.Tests.Freeze;

namespace Notrelix.Domain.Tests.Automation.Rules;

public class AutomationRuleMutationAtomicityTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _actorId = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    private AutomationConfiguration CreateConfig() => AutomationConfiguration.Create(
        AutomationTriggerDefinition.Create("FieldChanged", """{"fieldId":"fld_123"}"""),
        AutomationActionDefinition.Create("SendEmail", """{"templateId":"tpl_1"}"""));

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Enable), MutationScenario.Audit, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Enable_ShouldUseTwoPhaseAudit()
    {
        var rule = AutomationRule.Create(_accountId, _workspaceId, "Rule", CreateConfig(), _actorId, _now);
        var before = rule.Version;
        rule.Enable(_actorId, _now);
        rule.Version.Should().Be(before + 1);
        rule.IsEnabled.Should().BeTrue();
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Enable), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Enable_WhenDeleted_ShouldThrow()
    {
        var rule = AutomationRule.Create(_accountId, _workspaceId, "Rule", CreateConfig(), _actorId, _now);
        rule.Delete(_actorId, _now);
        var act = () => rule.Enable(_actorId, _now);
        act.Should().Throw<DomainException>();
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Enable), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Enable_WhenAlreadyEnabled_ShouldBeNoOp()
    {
        var rule = AutomationRule.Create(_accountId, _workspaceId, "Rule", CreateConfig(), _actorId, _now);
        rule.Enable(_actorId, _now);
        var before = rule.Version;
        rule.Enable(_actorId, _now);
        rule.Version.Should().Be(before);
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Disable), MutationScenario.Audit, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Disable_ShouldUseTwoPhaseAudit()
    {
        var rule = AutomationRule.Create(_accountId, _workspaceId, "Rule", CreateConfig(), _actorId, _now);
        rule.Enable(_actorId, _now);
        var before = rule.Version;
        rule.Disable(_actorId, _now);
        rule.Version.Should().Be(before + 1);
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Disable), MutationScenario.Invalid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Disable_WhenDeleted_ShouldThrow()
    {
        var rule = AutomationRule.Create(_accountId, _workspaceId, "Rule", CreateConfig(), _actorId, _now);
        rule.Delete(_actorId, _now);
        var act = () => rule.Disable(_actorId, _now);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateConfiguration_ShouldUseTwoPhaseAudit()
    {
        var rule = AutomationRule.Create(_accountId, _workspaceId, "Rule", CreateConfig(), _actorId, _now);
        var newConfig = AutomationConfiguration.Create(
            AutomationTriggerDefinition.Create("ItemCreated"),
            AutomationActionDefinition.Create("CreateItem"));
        var before = rule.Version;
        rule.UpdateConfiguration(newConfig, _actorId, _now);
        rule.Version.Should().Be(before + 1);
        rule.Configuration.Should().Be(newConfig);
    }

    [Fact]
    public void UpdateConfiguration_WhenDeleted_ShouldThrow()
    {
        var rule = AutomationRule.Create(_accountId, _workspaceId, "Rule", CreateConfig(), _actorId, _now);
        rule.Delete(_actorId, _now);
        var act = () => rule.UpdateConfiguration(CreateConfig(), _actorId, _now);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateConfiguration_WithSameConfig_ShouldBeNoOp()
    {
        var config = CreateConfig();
        var rule = AutomationRule.Create(_accountId, _workspaceId, "Rule", config, _actorId, _now);
        var before = rule.Version;
        rule.UpdateConfiguration(config, _actorId, _now);
        rule.Version.Should().Be(before);
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Delete), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_ShouldSetDeleted()
    {
        var rule = AutomationRule.Create(_accountId, _workspaceId, "Rule", CreateConfig(), _actorId, _now);
        rule.Delete(_actorId, _now);
        rule.IsDeleted.Should().BeTrue();
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Delete), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset), typeof(string))]
    [Fact]
    public void Delete_WhenAlreadyDeleted_ShouldBeNoOp()
    {
        var rule = AutomationRule.Create(_accountId, _workspaceId, "Rule", CreateConfig(), _actorId, _now);
        rule.Delete(_actorId, _now);
        var before = rule.Version;
        rule.Delete(_actorId, _now);
        rule.Version.Should().Be(before);
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Restore), MutationScenario.Lifecycle, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_ShouldClearDeleted()
    {
        var rule = AutomationRule.Create(_accountId, _workspaceId, "Rule", CreateConfig(), _actorId, _now);
        rule.Delete(_actorId, _now);
        rule.Restore(_actorId, _now);
        rule.IsDeleted.Should().BeFalse();
    }

    [CoversMutation(typeof(AutomationRule), nameof(AutomationRule.Restore), MutationScenario.NoOp, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Restore_WhenNotDeleted_ShouldBeNoOp()
    {
        var rule = AutomationRule.Create(_accountId, _workspaceId, "Rule", CreateConfig(), _actorId, _now);
        var before = rule.Version;
        rule.Restore(_actorId, _now);
        rule.Version.Should().Be(before);
    }
}
