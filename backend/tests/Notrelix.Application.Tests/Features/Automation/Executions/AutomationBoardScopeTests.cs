using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Messaging;
using Notrelix.Application.Events.Automation;
using Notrelix.Application.Features.Automation;
using Notrelix.Application.Features.Automation.Events;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Application.Tests.Features.Automation.Executions;

/// <summary>AU-1/AU-2: a rule belongs to a board, its trigger filter is evaluated, and its action targets the triggering item.</summary>
public class AutomationBoardScopeTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid UserId = Guid.CreateVersion7();
    private static readonly Guid BoardId = Guid.CreateVersion7();
    private static readonly Guid DoneGroup = Guid.CreateVersion7();
    private static readonly Guid ArchiveGroup = Guid.CreateVersion7();

    private sealed class Clock(DateTimeOffset now) : Notrelix.Application.Common.Time.IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    private static TestAutomationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<TestAutomationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static AutomationRule NewRule(
        TestAutomationDbContext context, Guid? boardId, string? triggerConfiguration)
    {
        var rule = AutomationRule.Create(
            AccountId, WorkspaceId, "rule",
            AutomationConfiguration.Create(
                AutomationTriggerDefinition.Create("ItemMovedToGroup", triggerConfiguration),
                AutomationActionDefinition.Create("MoveItem", $$"""{"targetGroupId":"{{ArchiveGroup}}"}""")),
            UserId, T0, boardId);
        rule.Enable(UserId, T0);
        context.AutomationRules.Add(rule);
        return rule;
    }

    private static AutomationTriggerContext Moved(Guid board, Guid group, Guid item) =>
        new(AccountId, WorkspaceId, "ItemMovedToGroup", Guid.CreateVersion7(), UserId,
            Guid.CreateVersion7(), null, T0.AddMinutes(30), board, item, group);

    private static async Task<(int executions, IntegrationEventCollector events)> RunAsync(
        TestAutomationDbContext context, AutomationTriggerContext trigger)
    {
        var events = new IntegrationEventCollector();
        var evaluator = new N8nAutomationRuleEvaluator(
            context, events, new Clock(T0.AddHours(1)), Options.Create(new AutomationGuardOptions()));
        await evaluator.ExecuteAsync(trigger, CancellationToken.None);
        await context.SaveChangesAsync();
        return (await context.AutomationExecutions.CountAsync(), events);
    }

    [Fact]
    public async Task BoardRule_IgnoresFactsOfAnotherBoard()
    {
        using var context = NewContext();
        NewRule(context, BoardId, $$"""{"groupId":"{{DoneGroup}}"}""");
        await context.SaveChangesAsync();

        var (executions, _) = await RunAsync(context, Moved(Guid.CreateVersion7(), DoneGroup, Guid.CreateVersion7()));

        executions.Should().Be(0);
    }

    [Fact]
    public async Task GroupFilter_MatchesOnlyTheGroupTheItemMovedInto()
    {
        using var context = NewContext();
        NewRule(context, BoardId, $$"""{"groupId":"{{DoneGroup}}"}""");
        await context.SaveChangesAsync();

        (await RunAsync(context, Moved(BoardId, ArchiveGroup, Guid.CreateVersion7()))).executions.Should().Be(0);
        (await RunAsync(context, Moved(BoardId, DoneGroup, Guid.CreateVersion7()))).executions.Should().Be(1);
    }

    [Fact]
    public async Task BoardRule_MovesTheTriggeringItem()
    {
        using var context = NewContext();
        NewRule(context, BoardId, $$"""{"groupId":"{{DoneGroup}}"}""");
        await context.SaveChangesAsync();
        var item = Guid.CreateVersion7();

        var (_, events) = await RunAsync(context, Moved(BoardId, DoneGroup, item));

        events.CapturePending().Events.OfType<AutomationMoveItemRequestedV1>().Should().ContainSingle()
            .Which.ItemId.Should().Be(item);
    }

    [Fact]
    public async Task RuleWithoutBoard_KeepsRunningWorkspaceWideOnItsConfiguredItem()
    {
        using var context = NewContext();
        NewRule(context, null, $$"""{"groupId":"{{DoneGroup}}"}""");
        await context.SaveChangesAsync();

        var (executions, events) = await RunAsync(context, Moved(BoardId, DoneGroup, Guid.CreateVersion7()));

        executions.Should().Be(1);
        events.CapturePending().Events.OfType<AutomationMoveItemRequestedV1>().Should().ContainSingle()
            .Which.ItemId.Should().BeNull();
    }
}
