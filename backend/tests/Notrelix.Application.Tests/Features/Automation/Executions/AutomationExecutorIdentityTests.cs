using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Messaging;
using Notrelix.Application.Events.Automation;
using Notrelix.Application.Features.Automation;
using Notrelix.Application.Features.Automation.Events;
using Notrelix.Application.Features.Automation.Executions.Services;
using Notrelix.Application.Features.Automation.Ports.WorkManagement;
using Notrelix.Application.Features.Automation.Rules.Commands.SetAutomationRuleEnabled;
using Notrelix.Application.Features.Automation.Rules.Services;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Application.Tests.Features.Automation.Executions;

/// <summary>
/// AUT-020: a rule runs as its bound executor, the executor is re-authorized on
/// every execution, and the rule stops (with a recorded reason) when the executor
/// loses standing. Orchestration semantics on EF InMemory; the real MoveItem
/// policy and PostgreSQL behavior are proved by the integration suite.
/// </summary>
public class AutomationExecutorIdentityTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid Creator = Guid.CreateVersion7();
    private static readonly Guid TriggerActor = Guid.CreateVersion7();
    private static readonly Guid ItemId = Guid.CreateVersion7();
    private static readonly Guid GroupId = Guid.CreateVersion7();

    private sealed class Clock(DateTimeOffset now) : Notrelix.Application.Common.Time.IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    private static TestAutomationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<TestAutomationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static AutomationRule MoveRule(TestAutomationDbContext context, Guid? executor = null, bool enable = true)
    {
        var rule = AutomationRule.Create(
            AccountId, WorkspaceId, "Move rule",
            AutomationConfiguration.Create(
                AutomationTriggerDefinition.Create("ItemCreated"),
                AutomationActionDefinition.Create("MoveItem", $$"""{"itemId":"{{ItemId}}","targetGroupId":"{{GroupId}}"}""")),
            executor ?? Creator, T0);
        if (enable) rule.Enable(Creator, T0);
        context.AutomationRules.Add(rule);
        return rule;
    }

    private static void ClearExecutor(AutomationRule rule) =>
        typeof(AutomationRule).GetProperty(nameof(AutomationRule.ExecutorUserId))!.SetValue(rule, null);

    private static AutomationExecution Queue(TestAutomationDbContext context, AutomationRule rule)
    {
        var execution = AutomationExecution.Create(AccountId, WorkspaceId, rule.Id, Guid.NewGuid(), T0);
        context.AutomationExecutions.Add(execution);
        return execution;
    }

    private static AutomationMoveItemRequestedV1 Message(AutomationExecution execution, AutomationRule rule) =>
        new(Guid.CreateVersion7(), execution.Id, rule.Id, AccountId, WorkspaceId, TriggerActor, T0, Guid.NewGuid(), null);

    private static AutomationMoveItemUseCase UseCase(TestAutomationDbContext context, Mock<IWorkActionPort> port) =>
        new(context, port.Object, new Clock(T0.AddMinutes(5)), new Mock<IOrderingConflictClassifier>().Object);

    private static Mock<IWorkActionPort> PortAllowing()
    {
        var port = new Mock<IWorkActionPort>();
        port.Setup(p => p.MoveItemAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkActionResult(ItemId, GroupId, "a"));
        return port;
    }

    [Fact]
    public async Task Create_BindsTheCreatorAsExecutor()
    {
        using var context = NewContext();
        var rule = MoveRule(context, enable: false);
        rule.ExecutorUserId.Should().Be(Creator);
        rule.HasExecutor.Should().BeTrue();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Execution_MovesAsTheExecutor_NotTheTriggerActor()
    {
        using var context = NewContext();
        var rule = MoveRule(context);
        var execution = Queue(context, rule);
        await context.SaveChangesAsync();
        var port = PortAllowing();

        var complete = await UseCase(context, port).ExecuteAsync(Message(execution, rule), CancellationToken.None);

        complete.Should().BeTrue();
        port.Verify(p => p.MoveItemAsync(ItemId, GroupId, execution.Id,
            It.Is<AutomationPrincipal>(x => x.ExecutorUserId == Creator && x.ExecutorUserId != TriggerActor),
            It.IsAny<CancellationToken>()), Times.Once);
        (await context.AutomationExecutions.SingleAsync()).Status.Should().Be(AutomationExecutionStatus.Succeeded);
        (await context.AutomationRules.SingleAsync()).Status.Should().Be(AutomationRuleStatus.Active);
    }

    [Fact]
    public async Task Execution_ExecutorLostPermission_DisablesRuleWithReason_AndNeverMoves()
    {
        using var context = NewContext();
        var rule = MoveRule(context);
        var execution = Queue(context, rule);
        await context.SaveChangesAsync();
        var port = PortAllowing();
        port.Setup(p => p.EnsureMoveItemAllowedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("denied"));

        var complete = await UseCase(context, port).ExecuteAsync(Message(execution, rule), CancellationToken.None);

        complete.Should().BeTrue();
        port.Verify(p => p.MoveItemAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()), Times.Never);
        var stored = await context.AutomationRules.SingleAsync();
        stored.Status.Should().Be(AutomationRuleStatus.Disabled);
        stored.DisabledReason.Should().Contain("no longer has permission");
        (await context.AutomationExecutions.SingleAsync()).Status.Should().Be(AutomationExecutionStatus.Failed);
    }

    [Fact]
    public async Task Execution_TargetNotVisibleToExecutor_DisablesRuleWithReason_AndNeverMoves()
    {
        using var context = NewContext();
        var rule = MoveRule(context);
        var execution = Queue(context, rule);
        await context.SaveChangesAsync();
        var port = PortAllowing();
        port.Setup(p => p.EnsureMoveItemAllowedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("BoardItem", ItemId));

        await UseCase(context, port).ExecuteAsync(Message(execution, rule), CancellationToken.None);

        port.Verify(p => p.MoveItemAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()), Times.Never);
        (await context.AutomationRules.SingleAsync()).Status.Should().Be(AutomationRuleStatus.Disabled);
    }

    [Fact]
    public async Task Execution_LegacyRuleWithoutExecutor_FailsClosed_AndDisables()
    {
        using var context = NewContext();
        var rule = MoveRule(context);
        ClearExecutor(rule);
        var execution = Queue(context, rule);
        await context.SaveChangesAsync();
        var port = PortAllowing();

        await UseCase(context, port).ExecuteAsync(Message(execution, rule), CancellationToken.None);

        port.VerifyNoOtherCalls();
        var stored = await context.AutomationRules.SingleAsync();
        stored.Status.Should().Be(AutomationRuleStatus.Disabled);
        stored.DisabledReason.Should().Contain("no executor");
        (await context.AutomationExecutions.SingleAsync()).Status.Should().Be(AutomationExecutionStatus.Failed);
    }

    [Fact]
    public async Task Evaluator_LegacyRuleWithoutExecutor_IsDisabledAtFirstEvaluation_AndStartsNothing()
    {
        using var context = NewContext();
        var rule = MoveRule(context);
        ClearExecutor(rule);
        await context.SaveChangesAsync();
        var evaluator = new N8nAutomationRuleEvaluator(context, new IntegrationEventCollector(), new Clock(T0.AddHours(1)), Options.Create(new AutomationGuardOptions()));

        await evaluator.ExecuteAsync(
            new AutomationTriggerContext(AccountId, WorkspaceId, "ItemCreated", Guid.CreateVersion7(), TriggerActor, Guid.NewGuid(), null, T0.AddMinutes(30)),
            CancellationToken.None);
        await context.SaveChangesAsync();

        (await context.AutomationExecutions.CountAsync()).Should().Be(0);
        var stored = await context.AutomationRules.SingleAsync();
        stored.Status.Should().Be(AutomationRuleStatus.Disabled);
        stored.DisabledReason.Should().Contain("no executor");
    }

    [Fact]
    public void Enable_LegacyRuleWithoutExecutor_IsRejected()
    {
        using var context = NewContext();
        var rule = MoveRule(context, enable: false);
        ClearExecutor(rule);

        var act = () => rule.Enable(Creator, T0);

        act.Should().Throw<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        rule.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task EnableHandler_ExecutorWithoutPermission_IsForbidden_AndRuleStaysDisabled()
    {
        using var context = NewContext();
        var rule = MoveRule(context, enable: false);
        await context.SaveChangesAsync();
        var port = new Mock<IWorkActionPort>();
        port.Setup(p => p.EnsureMoveItemAllowedAsync(ItemId, GroupId,
                It.Is<AutomationPrincipal>(x => x.ExecutorUserId == Creator), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ForbiddenException("denied"));
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.UserId).Returns(TriggerActor);
        var handler = new SetAutomationRuleEnabledCommandHandler(
            context, currentUser.Object, new Clock(T0), new AutomationExecutorAuthority(port.Object));

        var act = () => handler.Handle(new SetAutomationRuleEnabledCommand(rule.Id, true), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        rule.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Authority_ForMoveItem_ChecksTheExecutorOnTheConfiguredTarget_AndSkipsWebhook()
    {
        var port = new Mock<IWorkActionPort>();
        var authority = new AutomationExecutorAuthority(port.Object);

        await authority.EnsureCanExecuteAsync(AccountId, WorkspaceId, Creator,
            AutomationActionDefinition.Create("Webhook", """{"webhookPath":"hook"}"""), CancellationToken.None);
        port.VerifyNoOtherCalls();

        await authority.EnsureCanExecuteAsync(AccountId, WorkspaceId, Creator,
            AutomationActionDefinition.Create("MoveItem", $$"""{"itemId":"{{ItemId}}","targetGroupId":"{{GroupId}}"}"""), CancellationToken.None);
        port.Verify(p => p.EnsureMoveItemAllowedAsync(ItemId, GroupId,
            It.Is<AutomationPrincipal>(x => x.ExecutorUserId == Creator && x.WorkspaceId == WorkspaceId && x.AccountId == AccountId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Authority_ForMoveItem_PropagatesTheTargetsRejection()
    {
        var port = new Mock<IWorkActionPort>();
        port.Setup(p => p.EnsureMoveItemAllowedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("BoardItem", ItemId));

        var act = () => new AutomationExecutorAuthority(port.Object).EnsureCanExecuteAsync(
            AccountId, WorkspaceId, Creator,
            AutomationActionDefinition.Create("MoveItem", $$"""{"itemId":"{{ItemId}}","targetGroupId":"{{GroupId}}"}"""), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static AutomationExecutorLifecycle Lifecycle(TestAutomationDbContext context) =>
        new(context, new Clock(T0.AddHours(2)), NullLogger<AutomationExecutorLifecycle>.Instance);

    [Fact]
    public async Task MemberRemoved_DisablesOnlyThatExecutorsActiveRules_WithReason()
    {
        using var context = NewContext();
        var mine = MoveRule(context);
        var other = MoveRule(context, executor: TriggerActor);
        other.Enable(TriggerActor, T0);
        await context.SaveChangesAsync();

        await Lifecycle(context).OnMemberRemovedAsync(AccountId, WorkspaceId, Creator, CancellationToken.None);
        await context.SaveChangesAsync();

        mine.Status.Should().Be(AutomationRuleStatus.Disabled);
        mine.DisabledReason.Should().Be(AutomationExecutorLifecycle.RemovedReason);
        other.Status.Should().Be(AutomationRuleStatus.Active);
    }

    [Fact]
    public async Task MemberRemoved_InAnotherWorkspace_DoesNotTouchTheRule()
    {
        using var context = NewContext();
        var rule = MoveRule(context);
        await context.SaveChangesAsync();

        await Lifecycle(context).OnMemberRemovedAsync(AccountId, Guid.CreateVersion7(), Creator, CancellationToken.None);

        rule.Status.Should().Be(AutomationRuleStatus.Active);
    }

    [Fact]
    public async Task MemberSuspended_DisablesTheExecutorsRules_WithReason()
    {
        using var context = NewContext();
        var rule = MoveRule(context);
        await context.SaveChangesAsync();

        await Lifecycle(context).OnMemberSuspendedAsync(AccountId, WorkspaceId, Creator, CancellationToken.None);

        rule.Status.Should().Be(AutomationRuleStatus.Disabled);
        rule.DisabledReason.Should().Be(AutomationExecutorLifecycle.SuspendedReason);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MemberRoleChanged_DisablesOnlyOnDemotion(bool isDemotion)
    {
        using var context = NewContext();
        var rule = MoveRule(context);
        await context.SaveChangesAsync();

        await Lifecycle(context).OnMemberRoleChangedAsync(AccountId, WorkspaceId, Creator, isDemotion, CancellationToken.None);

        (rule.Status == AutomationRuleStatus.Disabled).Should().Be(isDemotion);
        if (isDemotion) rule.DisabledReason.Should().Be(AutomationExecutorLifecycle.DemotedReason);
    }
}
