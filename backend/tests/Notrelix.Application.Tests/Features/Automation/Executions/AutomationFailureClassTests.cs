using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Events.Automation;
using Notrelix.Application.Features.Automation.Executions.Services;
using Notrelix.Application.Features.Automation.Ports.WorkManagement;
using Notrelix.Application.Features.Automation.Rules.Services;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Application.Tests.Features.Automation.Executions;

/// <summary>
/// UC-AUT-I04: each way a rule's execution can fail or be retried lands on one stable failure class. A terminal class
/// ends the execution and is never retried; only a technical failure is retried under the same execution identity.
/// </summary>
public class AutomationFailureClassTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid BoardId = Guid.CreateVersion7();
    private static readonly Guid Executor = Guid.CreateVersion7();
    private static readonly Guid ItemId = Guid.CreateVersion7();
    private static readonly Guid Option = Guid.CreateVersion7();

    private sealed class Clock(DateTimeOffset now) : Notrelix.Application.Common.Time.IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    private static TestAutomationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<TestAutomationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static AutomationConfiguration Config() =>
        AutomationRuleAssembler.Assemble(
            "ItemCreated", "{}", BoardId,
            [new RuleActionInput("ChangeStatus", $$"""{"optionId":"{{Option}}"}""")], null);

    private static async Task<(AutomationRule rule, AutomationExecution execution)> StageAsync(TestAutomationDbContext context, bool enable = true)
    {
        var rule = AutomationRule.Create(AccountId, WorkspaceId, "rule", Config(), Executor, T0, BoardId);
        if (enable) rule.Enable(Executor, T0);
        context.AutomationRules.Add(rule);
        var execution = AutomationExecution.Create(
            AccountId, WorkspaceId, rule.Id, Guid.NewGuid(), T0, null, 0,
            rule.ConfigurationVersion, AutomationRuleSnapshot.From(rule).Serialize());
        context.AutomationExecutions.Add(execution);
        await context.SaveChangesAsync();
        return (rule, execution);
    }

    private static AutomationMoveItemUseCase UseCase(TestAutomationDbContext context, Mock<IWorkActionPort> port) =>
        new(context, port.Object, new Clock(T0.AddMinutes(5)), new Mock<IOrderingConflictClassifier>().Object, null, null);

    private static AutomationMoveItemRequestedV1 Message(AutomationExecution execution, AutomationRule rule) =>
        new(Guid.CreateVersion7(), execution.Id, rule.Id, AccountId, WorkspaceId, Executor, T0, Guid.NewGuid(), null, ItemId);

    private static Mock<IWorkActionPort> PortThrowing(Exception exception)
    {
        var port = new Mock<IWorkActionPort>();
        port.Setup(p => p.ChangeStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
        return port;
    }

    private static async Task<AutomationExecution> Saved(TestAutomationDbContext context, Guid id) =>
        await context.AutomationExecutions.AsNoTracking().SingleAsync(e => e.Id == id);

    [Fact]
    public async Task TargetRejection_FailsAsTargetRejected_AndIsNotRetried()
    {
        using var context = NewContext();
        var (rule, execution) = await StageAsync(context);
        var port = PortThrowing(new Notrelix.Domain.Common.Exceptions.BusinessRuleException("x", "rejected"));

        var complete = await UseCase(context, port).ExecuteAsync(Message(execution, rule), default);

        complete.Should().BeTrue();
        var saved = await Saved(context, execution.Id);
        saved.Status.Should().Be(AutomationExecutionStatus.Failed);
        saved.FailureClass.Should().Be(AutomationFailureClass.TargetRejected);
    }

    [Fact]
    public async Task LostExecutorPermission_FailsAsPermissionRevoked_AndStopsTheRule()
    {
        using var context = NewContext();
        var (rule, execution) = await StageAsync(context);
        var port = PortThrowing(new ForbiddenException("no permission"));

        var complete = await UseCase(context, port).ExecuteAsync(Message(execution, rule), default);

        complete.Should().BeTrue();
        var saved = await Saved(context, execution.Id);
        saved.Status.Should().Be(AutomationExecutionStatus.Failed);
        saved.FailureClass.Should().Be(AutomationFailureClass.PermissionRevoked);
        (await context.AutomationRules.AsNoTracking().SingleAsync(r => r.Id == rule.Id)).IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task GoneTarget_FailsAsTargetGone_AndIsNotRetried()
    {
        using var context = NewContext();
        var (rule, execution) = await StageAsync(context);
        var port = PortThrowing(new NotFoundException("target group"));

        var complete = await UseCase(context, port).ExecuteAsync(Message(execution, rule), default);

        complete.Should().BeTrue();
        var saved = await Saved(context, execution.Id);
        saved.Status.Should().Be(AutomationExecutionStatus.Failed);
        saved.FailureClass.Should().Be(AutomationFailureClass.TargetGone);
    }

    [Fact]
    public async Task TechnicalFailure_IsRetryable_RequeuesUnderTheSameExecution_AndRecordsTransient()
    {
        using var context = NewContext();
        var (rule, execution) = await StageAsync(context);
        var port = PortThrowing(new InvalidOperationException("database timeout"));

        var complete = await UseCase(context, port).ExecuteAsync(Message(execution, rule), default);

        complete.Should().BeFalse();
        var saved = await Saved(context, execution.Id);
        saved.Status.Should().Be(AutomationExecutionStatus.Queued);
        saved.AttemptCount.Should().Be(1);
        saved.FailureClass.Should().Be(AutomationFailureClass.Transient);
    }

    [Fact]
    public async Task DisabledRule_FailsAsRuleInactive()
    {
        using var context = NewContext();
        var (rule, execution) = await StageAsync(context, enable: false);
        rule.Disable(Executor, T0);
        await context.SaveChangesAsync();

        var complete = await UseCase(context, new Mock<IWorkActionPort>()).ExecuteAsync(Message(execution, rule), default);

        complete.Should().BeTrue();
        var saved = await Saved(context, execution.Id);
        saved.Status.Should().Be(AutomationExecutionStatus.Failed);
        saved.FailureClass.Should().Be(AutomationFailureClass.RuleInactive);
    }

    [Fact]
    public async Task MissingRule_FailsAsTargetGone()
    {
        using var context = NewContext();
        var (rule, execution) = await StageAsync(context);
        context.AutomationRules.Remove(await context.AutomationRules.SingleAsync(r => r.Id == rule.Id));
        await context.SaveChangesAsync();

        var complete = await UseCase(context, new Mock<IWorkActionPort>()).ExecuteAsync(Message(execution, rule), default);

        complete.Should().BeTrue();
        var saved = await Saved(context, execution.Id);
        saved.FailureClass.Should().Be(AutomationFailureClass.TargetGone);
    }
}
