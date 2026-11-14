using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Features.Automation.Executions.Commands.CancelAutomationExecution;
using Notrelix.Domain.Automation;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Application.Tests.Features.Automation.Executions;

/// <summary>
/// UC-AUT-H04: the cancel command ends a Queued execution of the named rule. A running or finished
/// execution is refused by the state machine, and an execution of another rule or board is not found.
/// </summary>
public sealed class CancelAutomationExecutionTests
{
    private static readonly Guid Workspace = Guid.CreateVersion7();
    private static readonly Guid Actor = Guid.CreateVersion7();
    private static readonly Guid Board = Guid.CreateVersion7();
    private static readonly DateTimeOffset T0 = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

    private static TestAutomationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<TestAutomationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static AutomationRule NewRule(TestAutomationDbContext context, Guid? boardId = null)
    {
        var rule = AutomationRule.Create(
            Guid.CreateVersion7(), Workspace, "rule",
            AutomationConfiguration.Create(
                AutomationTriggerDefinition.Create("ItemCreated", "{}"),
                AutomationActionDefinition.Create("Webhook", """{"webhookPath":"some-hook"}""")),
            Actor, T0, boardId);
        context.AutomationRules.Add(rule);
        return rule;
    }

    private static AutomationExecution NewExecution(TestAutomationDbContext context, AutomationRule rule)
    {
        var execution = AutomationExecution.Create(rule.AccountId, Workspace, rule.Id, Guid.CreateVersion7(), T0);
        context.AutomationExecutions.Add(execution);
        return execution;
    }

    private static CancelAutomationExecutionCommandHandler Handler(TestAutomationDbContext context) =>
        new(context,
            Mock.Of<ICurrentRequestContext>(r => r.UserId == Actor),
            Mock.Of<IDateTimeProvider>(c => c.UtcNow == T0));

    [Fact]
    public async Task Cancel_QueuedExecution_EndsCancelled()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        var execution = NewExecution(context, rule);
        await context.SaveChangesAsync();

        var result = await Handler(context).Handle(new CancelAutomationExecutionCommand(rule.Id, execution.Id), default);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();
        var saved = await context.AutomationExecutions.AsNoTracking().SingleAsync(e => e.Id == execution.Id);
        saved.Status.Should().Be(AutomationExecutionStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_RunningExecution_IsRefusedAndStaysRunning()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        var execution = NewExecution(context, rule);
        execution.Start(T0);
        await context.SaveChangesAsync();

        var act = () => Handler(context).Handle(new CancelAutomationExecutionCommand(rule.Id, execution.Id), default);

        (await act.Should().ThrowAsync<DomainBusinessRuleException>())
            .Which.RuleCode.Should().Be(AutomationRuleCodes.Automation_Execution_CannotCancelUnlessQueued);
        var saved = await context.AutomationExecutions.AsNoTracking().SingleAsync(e => e.Id == execution.Id);
        saved.Status.Should().Be(AutomationExecutionStatus.Running);
    }

    [Fact]
    public async Task Cancel_FinishedExecution_IsRefused()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        var execution = NewExecution(context, rule);
        execution.Start(T0);
        execution.Succeed(T0);
        await context.SaveChangesAsync();

        var act = () => Handler(context).Handle(new CancelAutomationExecutionCommand(rule.Id, execution.Id), default);

        await act.Should().ThrowAsync<DomainBusinessRuleException>();
    }

    [Fact]
    public async Task Cancel_ExecutionOfAnotherRule_IsNotFound()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        var other = NewRule(context);
        var execution = NewExecution(context, other);
        await context.SaveChangesAsync();

        var act = () => Handler(context).Handle(new CancelAutomationExecutionCommand(rule.Id, execution.Id), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Cancel_WithBoardClaimOfAnotherBoard_IsNotFound()
    {
        using var context = NewContext();
        var rule = NewRule(context, boardId: Board);
        var execution = NewExecution(context, rule);
        await context.SaveChangesAsync();

        var act = () => Handler(context).Handle(new CancelAutomationExecutionCommand(rule.Id, execution.Id, Guid.CreateVersion7()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Cancel_WithMatchingBoardClaim_Succeeds()
    {
        using var context = NewContext();
        var rule = NewRule(context, boardId: Board);
        var execution = NewExecution(context, rule);
        await context.SaveChangesAsync();

        var result = await Handler(context).Handle(new CancelAutomationExecutionCommand(rule.Id, execution.Id, Board), default);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Cancel_UnknownExecution_IsNotFound()
    {
        using var context = NewContext();
        var rule = NewRule(context);
        await context.SaveChangesAsync();

        var act = () => Handler(context).Handle(new CancelAutomationExecutionCommand(rule.Id, Guid.CreateVersion7()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
