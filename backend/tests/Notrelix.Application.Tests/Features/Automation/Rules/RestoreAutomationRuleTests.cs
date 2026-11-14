using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Automation.Rules.Commands.RestoreAutomationRule;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;
using Notrelix.Application.Tests.Features.Automation.Executions;

namespace Notrelix.Application.Tests.Features.Automation.Rules;

/// <summary>
/// UC-AUT-A07: restoring a rule deleted less than 90 days ago brings it back Disabled and takes no plan slot.
/// A live rule is a no-op, and a rule deleted longer ago than the history retention is not found.
/// </summary>
public sealed class RestoreAutomationRuleTests
{
    private static readonly Guid Workspace = Guid.CreateVersion7();
    private static readonly Guid Actor = Guid.CreateVersion7();
    private static readonly Guid Board = Guid.CreateVersion7();
    private static readonly DateTimeOffset T0 = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

    private static TestAutomationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<TestAutomationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static AutomationRule NewRule(TestAutomationDbContext context, Guid? boardId = null, bool enabled = true)
    {
        var rule = AutomationRule.Create(
            Guid.CreateVersion7(), Workspace, "rule",
            AutomationConfiguration.Create(
                AutomationTriggerDefinition.Create("ItemCreated", "{}"),
                AutomationActionDefinition.Create("Webhook", """{"webhookPath":"some-hook"}""")),
            Actor, T0, boardId);
        if (enabled) rule.Enable(Actor, T0);
        context.AutomationRules.Add(rule);
        return rule;
    }

    private static RestoreAutomationRuleCommandHandler Handler(TestAutomationDbContext context, DateTimeOffset now) =>
        new(context,
            Mock.Of<ICurrentRequestContext>(r => r.UserId == Actor),
            Mock.Of<IDateTimeProvider>(c => c.UtcNow == now));

    [Fact]
    public async Task Restore_RuleDeletedTenDaysAgo_ComesBackDisabled()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        rule.Remove(Actor, T0);
        await context.SaveChangesAsync();

        var result = await Handler(context, T0.AddDays(10)).Handle(new RestoreAutomationRuleCommand(rule.Id), default);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();
        var saved = await context.AutomationRules.AsNoTracking().SingleAsync(r => r.Id == rule.Id);
        saved.Status.Should().Be(AutomationRuleStatus.Disabled);
        saved.IsRemoved.Should().BeFalse();
    }

    [Fact]
    public async Task Restore_RuleDeletedMoreThanNinetyDaysAgo_IsNotFound()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        rule.Remove(Actor, T0);
        await context.SaveChangesAsync();

        var act = () => Handler(context, T0.AddDays(91)).Handle(new RestoreAutomationRuleCommand(rule.Id), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Restore_LiveRule_IsNoOp()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        await context.SaveChangesAsync();

        var result = await Handler(context, T0).Handle(new RestoreAutomationRuleCommand(rule.Id), default);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();
        var saved = await context.AutomationRules.AsNoTracking().SingleAsync(r => r.Id == rule.Id);
        saved.Status.Should().Be(AutomationRuleStatus.Active);
    }

    [Fact]
    public async Task Restore_WithBoardClaimOfAnotherBoard_IsNotFound()
    {
        using var context = NewContext();
        var rule = NewRule(context, boardId: Board);
        rule.Remove(Actor, T0);
        await context.SaveChangesAsync();

        var act = () => Handler(context, T0.AddDays(1)).Handle(new RestoreAutomationRuleCommand(rule.Id, Guid.CreateVersion7()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Restore_UnknownRule_IsNotFound()
    {
        using var context = NewContext();

        var act = () => Handler(context, T0).Handle(new RestoreAutomationRuleCommand(Guid.CreateVersion7()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
