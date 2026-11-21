using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Messaging;
using Notrelix.Application.Features.Accounts.Public.Status;
using Notrelix.Application.Features.Automation.Events;
using Notrelix.Application.Features.Automation.Rules.Services;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Automation.Rules;

namespace Notrelix.Application.Tests.Features.Automation.Executions;

/// <summary>
/// A suspended or closed Account is read-only: a rule whose trigger fires is not run, nothing is dispatched, and
/// the fact is kept as a Skipped execution with the reason <c>account.suspended</c> (not a failure, so it never
/// counts toward the consecutive-failure limit). An active Account runs the rule as before, and a redelivery of the
/// same fact records nothing twice.
/// </summary>
public sealed class AutomationAccountSuspensionTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid BoardId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly Guid TargetOption = Guid.CreateVersion7();

    private sealed class Clock(DateTimeOffset now) : Notrelix.Application.Common.Time.IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    private static TestAutomationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<TestAutomationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static async Task<AutomationRule> StageRuleAsync(TestAutomationDbContext context, string actionType, string actionJson, Guid? boardId = null)
    {
        var configuration = AutomationRuleAssembler.Assemble("ItemCreated", "{}", boardId, [new RuleActionInput(actionType, actionJson)], null);
        var rule = AutomationRule.Create(AccountId, WorkspaceId, "rule", configuration, UserId, T0, boardId);
        rule.Enable(UserId, T0);
        context.AutomationRules.Add(rule);
        await context.SaveChangesAsync();
        return rule;
    }

    private static AutomationTriggerContext Trigger(Guid? sourceEventId = null) =>
        new(AccountId, WorkspaceId, "ItemCreated", sourceEventId ?? Guid.CreateVersion7(), UserId, Guid.CreateVersion7(), null,
            T0.AddMinutes(30), BoardId, Guid.CreateVersion7());

    private static N8nAutomationRuleEvaluator Evaluator(TestAutomationDbContext context, IIntegrationEventCollector events, AccountStatusFact status)
    {
        var account = new Mock<IAccountStatusFacts>();
        account.Setup(a => a.GetAsync(AccountId, It.IsAny<CancellationToken>())).ReturnsAsync(status);
        return new(context, events, new Clock(T0.AddHours(1)), null, null, null, account.Object);
    }

    [Theory]
    [InlineData(AccountStatusFact.Suspended)]
    [InlineData(AccountStatusFact.Closed)]
    public async Task ASuspendedOrClosedAccount_SkipsTheRun_RecordsTheReason_AndDispatchesNothing(AccountStatusFact status)
    {
        using var context = NewContext();
        var rule = await StageRuleAsync(context, "ChangeStatus", $$"""{"optionId":"{{TargetOption}}"}""", BoardId);
        var events = new IntegrationEventCollector();

        await Evaluator(context, events, status).ExecuteAsync(Trigger(), CancellationToken.None);
        await context.SaveChangesAsync();

        var execution = (await context.AutomationExecutions.Where(e => e.RuleId == rule.Id).ToListAsync()).Should().ContainSingle().Subject;
        execution.Status.Should().Be(AutomationExecutionStatus.Skipped);
        execution.Error.Should().Be("account.suspended");
        execution.FailureClass.Should().BeNull("a suspended Account is not a failure of the rule");
        events.CapturePending().Events.Should().BeEmpty();
        (await context.AutomationRules.AsNoTracking().SingleAsync(r => r.Id == rule.Id)).IsEnabled.Should().BeTrue("nothing is deleted or disabled");
    }

    [Fact]
    public async Task ASuspendedAccount_AlsoSkipsAWebhookRule_WithoutQueueingTheDispatch()
    {
        using var context = NewContext();
        var rule = await StageRuleAsync(context, "Webhook", """{"webhookPath":"hooks/notify"}""");
        var events = new IntegrationEventCollector();

        await Evaluator(context, events, AccountStatusFact.Suspended).ExecuteAsync(Trigger(), CancellationToken.None);
        await context.SaveChangesAsync();

        (await context.AutomationExecutions.Where(e => e.RuleId == rule.Id).ToListAsync())
            .Should().ContainSingle().Which.Error.Should().Be("account.suspended");
        events.CapturePending().Events.Should().BeEmpty();
    }

    [Fact]
    public async Task AnActiveAccount_StillRunsTheRule()
    {
        using var context = NewContext();
        var rule = await StageRuleAsync(context, "ChangeStatus", $$"""{"optionId":"{{TargetOption}}"}""", BoardId);
        var events = new IntegrationEventCollector();

        await Evaluator(context, events, AccountStatusFact.Active).ExecuteAsync(Trigger(), CancellationToken.None);
        await context.SaveChangesAsync();

        (await context.AutomationExecutions.Where(e => e.RuleId == rule.Id).ToListAsync())
            .Should().ContainSingle().Which.Status.Should().Be(AutomationExecutionStatus.Queued);
    }

    [Fact]
    public async Task ARedeliveryOfTheSameFact_RecordsTheSkipOnlyOnce()
    {
        using var context = NewContext();
        var rule = await StageRuleAsync(context, "ChangeStatus", $$"""{"optionId":"{{TargetOption}}"}""", BoardId);
        var trigger = Trigger(Guid.CreateVersion7());

        for (var i = 0; i < 2; i++)
        {
            await Evaluator(context, new IntegrationEventCollector(), AccountStatusFact.Suspended).ExecuteAsync(trigger, CancellationToken.None);
            await context.SaveChangesAsync();
        }

        (await context.AutomationExecutions.CountAsync(e => e.RuleId == rule.Id)).Should().Be(1);
    }
}
