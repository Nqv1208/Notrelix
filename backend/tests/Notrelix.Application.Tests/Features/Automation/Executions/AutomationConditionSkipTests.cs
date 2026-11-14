using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Messaging;
using Notrelix.Application.Features.Automation.Events;
using Notrelix.Application.Features.Automation.Ports.WorkManagement;
using Notrelix.Application.Features.Automation.Rules.Services;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Automation.Rules;

namespace Notrelix.Application.Tests.Features.Automation.Executions;

/// <summary>
/// UC-AUT-C02: a rule whose conditions are evaluated and not met keeps a Skipped execution and dispatches nothing;
/// a condition that is met starts the execution as before, and a condition that cannot be evaluated records nothing.
/// </summary>
public sealed class AutomationConditionSkipTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid BoardId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly Guid ItemId = Guid.CreateVersion7();
    private static readonly Guid WantedStatus = Guid.CreateVersion7();
    private static readonly Guid OtherStatus = Guid.CreateVersion7();
    private static readonly Guid TargetOption = Guid.CreateVersion7();

    private sealed class Clock(DateTimeOffset now) : Notrelix.Application.Common.Time.IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    private static TestAutomationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<TestAutomationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static async Task<AutomationRule> StageRuleAsync(TestAutomationDbContext context)
    {
        var condition = $$"""{"all":[{"type":"status","optionId":"{{WantedStatus}}"}]}""";
        var configuration = AutomationRuleAssembler.Assemble(
            "ItemCreated", "{}", BoardId,
            [new RuleActionInput("ChangeStatus", $$"""{"optionId":"{{TargetOption}}"}""")],
            condition);
        var rule = AutomationRule.Create(AccountId, WorkspaceId, "rule", configuration, UserId, T0, BoardId);
        rule.Enable(UserId, T0);
        context.AutomationRules.Add(rule);
        await context.SaveChangesAsync();
        return rule;
    }

    private static AutomationTriggerContext Trigger(Guid? itemId) =>
        new(AccountId, WorkspaceId, "ItemCreated", Guid.CreateVersion7(), UserId, Guid.CreateVersion7(), null,
            T0.AddMinutes(30), BoardId, itemId);

    private static Mock<IWorkActionPort> FactsWithStatus(Guid statusOptionId)
    {
        var port = new Mock<IWorkActionPort>();
        port.Setup(p => p.GetItemFactsAsync(ItemId, BoardId, It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkActionItemFacts(Guid.NewGuid(), statusOptionId, "Task", new Dictionary<Guid, string>()));
        return port;
    }

    private static N8nAutomationRuleEvaluator Evaluator(TestAutomationDbContext context, IIntegrationEventCollector events, IWorkActionPort port) =>
        new(context, events, new Clock(T0.AddHours(1)), null, null, port);

    [Fact]
    public async Task ConditionNotMet_KeepsASkippedExecution_AndDispatchesNothing()
    {
        using var context = NewContext();
        var rule = await StageRuleAsync(context);
        var events = new IntegrationEventCollector();

        await Evaluator(context, events, FactsWithStatus(OtherStatus).Object).ExecuteAsync(Trigger(ItemId), CancellationToken.None);
        await context.SaveChangesAsync();

        var executions = await context.AutomationExecutions.Where(e => e.RuleId == rule.Id).ToListAsync();
        executions.Should().ContainSingle().Which.Status.Should().Be(AutomationExecutionStatus.Skipped);
        executions[0].FailureClass.Should().BeNull();
        executions[0].Error.Should().BeNull();
        events.CapturePending().Events.Should().BeEmpty();
        (await context.AutomationRules.AsNoTracking().SingleAsync(r => r.Id == rule.Id)).IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task ConditionMet_StillStartsAQueuedExecution()
    {
        using var context = NewContext();
        var rule = await StageRuleAsync(context);
        var events = new IntegrationEventCollector();

        await Evaluator(context, events, FactsWithStatus(WantedStatus).Object).ExecuteAsync(Trigger(ItemId), CancellationToken.None);
        await context.SaveChangesAsync();

        var executions = await context.AutomationExecutions.Where(e => e.RuleId == rule.Id).ToListAsync();
        executions.Should().ContainSingle().Which.Status.Should().Be(AutomationExecutionStatus.Queued);
        events.CapturePending().Events.Should().ContainSingle();
    }

    [Fact]
    public async Task ConditionThatCannotBeEvaluated_RecordsNothing()
    {
        using var context = NewContext();
        var rule = await StageRuleAsync(context);
        var events = new IntegrationEventCollector();

        await Evaluator(context, events, FactsWithStatus(OtherStatus).Object).ExecuteAsync(Trigger(itemId: null), CancellationToken.None);
        await context.SaveChangesAsync();

        (await context.AutomationExecutions.Where(e => e.RuleId == rule.Id).CountAsync()).Should().Be(0);
        events.CapturePending().Events.Should().BeEmpty();
    }
}
