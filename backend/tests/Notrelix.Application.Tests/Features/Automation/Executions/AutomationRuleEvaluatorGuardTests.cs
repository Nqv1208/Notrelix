using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Messaging;
using Notrelix.Application.Features.Automation;
using Notrelix.Application.Features.Automation.Events;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Application.Tests.Features.Automation.Executions;

/// <summary>AUT-022: causation hop guard, per-rule rate limit and failure circuit breaker.</summary>
public class AutomationRuleEvaluatorGuardTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();

    private sealed class Clock(DateTimeOffset now) : Notrelix.Application.Common.Time.IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    private static TestAutomationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<TestAutomationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static N8nAutomationRuleEvaluator Evaluator(TestAutomationDbContext context, AutomationGuardOptions? options = null, DateTimeOffset? now = null) =>
        new(context, new IntegrationEventCollector(), new Clock(now ?? T0.AddHours(1)), Options.Create(options ?? new AutomationGuardOptions()));

    private static AutomationRule NewRule(TestAutomationDbContext context, string name = "rule")
    {
        var rule = AutomationRule.Create(
            AccountId, WorkspaceId, name,
            AutomationConfiguration.Create(
                AutomationTriggerDefinition.Create("ItemCreated"),
                AutomationActionDefinition.Create("Webhook", """{"webhookPath":"hook"}""")),
            UserId, T0);
        rule.Enable(UserId, T0);
        context.AutomationRules.Add(rule);
        return rule;
    }

    private static AutomationTriggerContext Trigger(Guid? causation, DateTimeOffset? at = null, Guid? workspace = null) =>
        new(AccountId, workspace ?? WorkspaceId, "ItemCreated", Guid.CreateVersion7(), UserId,
            Guid.CreateVersion7(), causation, at ?? T0.AddMinutes(30));

    private static async Task<List<AutomationExecution>> ExecutionsAsync(TestAutomationDbContext context, Guid ruleId)
    {
        await context.SaveChangesAsync();
        return await context.AutomationExecutions.Where(e => e.RuleId == ruleId).ToListAsync();
    }

    private static AutomationExecution CausedBy(TestAutomationDbContext context, AutomationRule rule, AutomationExecution? parent, DateTimeOffset at)
    {
        var execution = AutomationExecution.Create(
            AccountId, WorkspaceId, rule.Id, Guid.CreateVersion7(), at,
            parent?.Id, parent is null ? 0 : parent.ChainDepth + 1);
        context.AutomationExecutions.Add(execution);
        return execution;
    }

    [Fact]
    public async Task FactWithoutCausation_StartsExecution_AsToday()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        await context.SaveChangesAsync();

        await Evaluator(context).ExecuteAsync(Trigger(null), CancellationToken.None);

        var executions = await ExecutionsAsync(context, rule.Id);
        executions.Should().ContainSingle().Which.ChainDepth.Should().Be(0);
        executions[0].CausationExecutionId.Should().BeNull();
    }

    [Fact]
    public async Task FactWithUnknownCausation_IsNotAutomationCaused()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        await context.SaveChangesAsync();

        await Evaluator(context).ExecuteAsync(Trigger(Guid.CreateVersion7()), CancellationToken.None);

        (await ExecutionsAsync(context, rule.Id)).Should().ContainSingle().Which.ChainDepth.Should().Be(0);
    }

    [Fact]
    public async Task FactCausedByOwnExecution_DoesNotRetriggerTheSameRule()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        var own = CausedBy(context, rule, null, T0.AddMinutes(1));
        own.Start(T0.AddMinutes(1));
        own.Succeed(T0.AddMinutes(1));
        await context.SaveChangesAsync();

        await Evaluator(context).ExecuteAsync(Trigger(own.Id), CancellationToken.None);

        (await ExecutionsAsync(context, rule.Id)).Should().ContainSingle("only the original execution exists; the self-trigger loop stops");
    }

    [Fact]
    public async Task FactCausedByAnotherRule_StartsChainedExecution_WithCausationAndDepth()
    {
        using var context = NewContext();
        var a = NewRule(context, "A");
        var b = NewRule(context, "B");
        var parent = CausedBy(context, a, null, T0.AddMinutes(1));
        await context.SaveChangesAsync();

        await Evaluator(context).ExecuteAsync(Trigger(parent.Id), CancellationToken.None);

        var chained = (await ExecutionsAsync(context, b.Id)).Should().ContainSingle().Subject;
        chained.CausationExecutionId.Should().Be(parent.Id);
        chained.ChainDepth.Should().Be(1);
    }

    [Fact]
    public async Task RuleToRuleLoop_IsBrokenWhenARuleAppearsUpstream()
    {
        using var context = NewContext();
        var a = NewRule(context, "A");
        var b = NewRule(context, "B");
        var aRun = CausedBy(context, a, null, T0.AddMinutes(1));
        var bRun = CausedBy(context, b, aRun, T0.AddMinutes(2));
        await context.SaveChangesAsync();

        // B's action produced a fact; A is upstream of B, so A must not fire again.
        await Evaluator(context).ExecuteAsync(Trigger(bRun.Id), CancellationToken.None);

        (await ExecutionsAsync(context, a.Id)).Should().ContainSingle("A -> B -> A is a loop");
        (await ExecutionsAsync(context, b.Id)).Should().ContainSingle("B caused this fact itself");
    }

    [Fact]
    public async Task ChainDeeperThanTheCap_DoesNotStart()
    {
        using var context = NewContext();
        var a = NewRule(context, "A");
        var c = NewRule(context, "C");
        AutomationExecution? parent = null;
        var rules = new[] { NewRule(context, "r1"), NewRule(context, "r2"), NewRule(context, "r3") };
        for (var i = 0; i < rules.Length; i++)
            parent = CausedBy(context, rules[i], parent, T0.AddMinutes(1 + i));
        await context.SaveChangesAsync();
        parent!.ChainDepth.Should().Be(2);

        await Evaluator(context, new AutomationGuardOptions { MaxChainDepth = 2 })
            .ExecuteAsync(Trigger(parent.Id), CancellationToken.None);

        (await ExecutionsAsync(context, a.Id)).Should().BeEmpty("depth 3 exceeds the cap of 2");
        (await ExecutionsAsync(context, c.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task CausationFromAnotherWorkspace_IsIgnored()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        var foreign = AutomationExecution.Create(AccountId, Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), T0);
        context.AutomationExecutions.Add(foreign);
        await context.SaveChangesAsync();

        await Evaluator(context).ExecuteAsync(Trigger(foreign.Id), CancellationToken.None);

        (await ExecutionsAsync(context, rule.Id)).Should().ContainSingle().Which.CausationExecutionId.Should().BeNull();
    }

    private static void AddFinished(TestAutomationDbContext context, AutomationRule rule, DateTimeOffset at, bool failed)
    {
        var execution = AutomationExecution.Create(AccountId, WorkspaceId, rule.Id, Guid.CreateVersion7(), at);
        execution.Start(at);
        if (failed) execution.Fail("x", at); else execution.Succeed(at);
        context.AutomationExecutions.Add(execution);
    }

    [Fact]
    public async Task RuleWhoseRecentExecutionsAllFailed_IsDisabled_AndStartsNothing()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        for (var i = 0; i < 5; i++) AddFinished(context, rule, T0.AddMinutes(1 + i), failed: true);
        await context.SaveChangesAsync();

        await Evaluator(context).ExecuteAsync(Trigger(null), CancellationToken.None);

        rule.IsEnabled.Should().BeFalse("five consecutive failures trip the circuit breaker");
        (await ExecutionsAsync(context, rule.Id)).Should().HaveCount(5);
    }

    [Fact]
    public async Task OneSuccessAmongRecentExecutions_KeepsTheRuleEnabled()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        for (var i = 0; i < 4; i++) AddFinished(context, rule, T0.AddMinutes(1 + i), failed: true);
        AddFinished(context, rule, T0.AddMinutes(10), failed: false);
        await context.SaveChangesAsync();

        await Evaluator(context).ExecuteAsync(Trigger(null), CancellationToken.None);

        rule.IsEnabled.Should().BeTrue();
        (await ExecutionsAsync(context, rule.Id)).Should().HaveCount(6);
    }

    [Fact]
    public async Task FailuresBeforeTheRuleWasReEnabled_DoNotCount()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        for (var i = 0; i < 5; i++) AddFinished(context, rule, T0.AddMinutes(1 + i), failed: true);
        rule.Disable(UserId, T0.AddMinutes(20));
        rule.Enable(UserId, T0.AddMinutes(21));
        await context.SaveChangesAsync();

        await Evaluator(context).ExecuteAsync(Trigger(null, T0.AddMinutes(40)), CancellationToken.None);

        rule.IsEnabled.Should().BeTrue();
        (await ExecutionsAsync(context, rule.Id)).Should().HaveCount(6);
    }

    [Fact]
    public async Task RuleThatExhaustedItsWindowAllowance_IsThrottled_ButStaysEnabled()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        var options = new AutomationGuardOptions { MaxExecutionsPerWindow = 3, RateWindowSeconds = 60 };
        for (var i = 0; i < 3; i++) AddFinished(context, rule, T0.AddMinutes(30).AddSeconds(-10 - i), failed: false);
        await context.SaveChangesAsync();

        await Evaluator(context, options).ExecuteAsync(Trigger(null, T0.AddMinutes(30)), CancellationToken.None);
        (await ExecutionsAsync(context, rule.Id)).Should().HaveCount(3, "the fourth execution within the window is not started");
        rule.IsEnabled.Should().BeTrue();

        await Evaluator(context, options).ExecuteAsync(Trigger(null, T0.AddMinutes(32)), CancellationToken.None);
        (await ExecutionsAsync(context, rule.Id)).Should().HaveCount(4, "outside the window the rule runs again");
    }

    [Fact]
    public void GuardOptionsValidator_RejectsOutOfRangeValues()
    {
        var validator = new AutomationGuardOptionsValidator();
        validator.Validate(null, new AutomationGuardOptions()).Succeeded.Should().BeTrue();
        validator.Validate(null, new AutomationGuardOptions { MaxChainDepth = 0 }).Failed.Should().BeTrue();
        validator.Validate(null, new AutomationGuardOptions { MaxExecutionsPerWindow = 0 }).Failed.Should().BeTrue();
        validator.Validate(null, new AutomationGuardOptions { RateWindowSeconds = 0 }).Failed.Should().BeTrue();
        validator.Validate(null, new AutomationGuardOptions { ConsecutiveFailureLimit = 0 }).Failed.Should().BeTrue();
    }
}
