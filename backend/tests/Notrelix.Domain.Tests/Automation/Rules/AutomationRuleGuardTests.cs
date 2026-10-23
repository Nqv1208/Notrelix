using FluentAssertions;
using Notrelix.Domain.Automation;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Domain.Tests.Automation;

public class AutomationRuleGuardTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid Account = Guid.CreateVersion7();
    private static readonly Guid Workspace = Guid.CreateVersion7();
    private static readonly Guid User = Guid.CreateVersion7();

    private static AutomationRule ActiveRule()
    {
        var rule = AutomationRule.Create(
            Account, Workspace, "r",
            AutomationConfiguration.Create(
                AutomationTriggerDefinition.Create("ItemCreated"),
                AutomationActionDefinition.Create("Webhook", """{"webhookPath":"hook"}""")),
            User, Now);
        rule.Enable(User, Now);
        return rule;
    }

    [Fact]
    public void Enable_DraftWebhookRuleWithUrlOnlyConfiguration_Throws()
    {
        var rule = AutomationRule.Create(
            Account, Workspace, "legacy",
            AutomationConfiguration.Create(
                AutomationTriggerDefinition.Create("ItemCreated"),
                AutomationActionDefinition.Create("Webhook", """{"url":"https://x"}""")),
            User, Now);

        var act = () => rule.Enable(User, Now);

        act.Should().Throw<BusinessRuleException>();
        rule.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void DisableAutomatically_DisablesActiveRule_RaisesDisabledFact_AndBumpsVersion()
    {
        var rule = ActiveRule();
        var version = rule.Version;
        var events = rule.DomainEvents.Count;

        rule.DisableAutomatically("executor lost access", Now.AddMinutes(1));

        rule.IsEnabled.Should().BeFalse();
        rule.DisabledReason.Should().Be("executor lost access");
        rule.Version.Should().Be(version + 1);
        rule.DomainEvents.Should().HaveCount(events + 1);
        rule.DomainEvents.Last().Should().BeOfType<AutomationRuleDisabledDomainEvent>();
    }

    [Fact]
    public void Create_BindsTheCreatorAsExecutor_AndEnableAfterAutomaticDisableClearsTheReason()
    {
        var rule = ActiveRule();
        rule.ExecutorUserId.Should().Be(User);

        rule.DisableAutomatically("executor lost access", Now.AddMinutes(1));
        rule.DisabledReason.Should().NotBeNull();

        rule.Enable(User, Now.AddMinutes(2));
        rule.DisabledReason.Should().BeNull();
    }

    [Fact]
    public void Enable_WithoutAnExecutor_IsRejected_AndManualDisableClearsTheReason()
    {
        var rule = ActiveRule();
        rule.DisableAutomatically("executor lost access", Now.AddMinutes(1));
        typeof(AutomationRule).GetProperty(nameof(AutomationRule.ExecutorUserId))!.SetValue(rule, null);

        var act = () => rule.Enable(User, Now.AddMinutes(2));

        act.Should().Throw<BusinessRuleException>()
            .Which.RuleCode.Should().Be(AutomationRuleCodes.Automation_Rule_MustHaveExecutor);
        rule.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void DisableAutomatically_OnNonActiveRule_IsNoOp()
    {
        var rule = ActiveRule();
        rule.Disable(User, Now.AddMinutes(1));
        var version = rule.Version;
        var events = rule.DomainEvents.Count;

        rule.DisableAutomatically("executor lost access", Now.AddMinutes(2));

        rule.Version.Should().Be(version);
        rule.DomainEvents.Should().HaveCount(events);
    }

    [Fact]
    public void Execution_CarriesCausationAndDepth()
    {
        var parent = Guid.CreateVersion7();
        var caused = AutomationExecution.Create(Account, Workspace, Guid.CreateVersion7(), Guid.CreateVersion7(), Now, parent, 2);
        caused.CausationExecutionId.Should().Be(parent);
        caused.ChainDepth.Should().Be(2);

        var root = AutomationExecution.Create(Account, Workspace, Guid.CreateVersion7(), Guid.CreateVersion7(), Now);
        root.CausationExecutionId.Should().BeNull();
        root.ChainDepth.Should().Be(0);
    }

    [Fact]
    public void Execution_InconsistentCausationAndDepth_Throws()
    {
        var act1 = () => AutomationExecution.Create(Account, Workspace, Guid.CreateVersion7(), Guid.CreateVersion7(), Now, Guid.CreateVersion7(), 0);
        var act2 = () => AutomationExecution.Create(Account, Workspace, Guid.CreateVersion7(), Guid.CreateVersion7(), Now, null, 1);
        act1.Should().Throw<ArgumentException>();
        act2.Should().Throw<ArgumentException>();
    }
}
