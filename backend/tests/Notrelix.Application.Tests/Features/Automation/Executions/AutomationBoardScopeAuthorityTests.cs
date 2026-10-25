using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Events.Automation;
using Notrelix.Application.Features.Automation.Executions.Services;
using Notrelix.Application.Features.Automation.Ports.WorkManagement;
using Notrelix.Application.Features.Automation.Rules.Services;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Application.Tests.Features.Automation.Executions;

/// <summary>AU-1/AU-2: a board-scoped rule is validated against its board whatever the action, and never moves another board's item.</summary>
public class AutomationBoardScopeAuthorityTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid Executor = Guid.CreateVersion7();
    private static readonly Guid BoardId = Guid.CreateVersion7();
    private static readonly Guid GroupId = Guid.CreateVersion7();
    private static readonly Guid ItemId = Guid.CreateVersion7();

    private sealed class Clock(DateTimeOffset now) : Notrelix.Application.Common.Time.IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    private static AutomationActionDefinition Move(string configuration) =>
        AutomationActionDefinition.Create("MoveItem", configuration);

    [Fact]
    public async Task FixedItemMoveOnABoardRule_IsRejected_BeforeAnyWorkCall()
    {
        var port = new Mock<IWorkActionPort>();
        var authority = new AutomationExecutorAuthority(port.Object);

        var act = () => authority.EnsureCanExecuteAsync(
            AccountId, WorkspaceId, Executor,
            Move($$"""{"itemId":"{{ItemId}}","targetGroupId":"{{GroupId}}"}"""),
            CancellationToken.None, BoardId);

        await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        port.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ForeignBoard_IsRejectedByTheWorkContext()
    {
        var port = new Mock<IWorkActionPort>();
        port.Setup(p => p.EnsureBoardGroupAsync(BoardId, GroupId, It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("Board", BoardId));
        var authority = new AutomationExecutorAuthority(port.Object);

        var act = () => authority.EnsureCanExecuteAsync(
            AccountId, WorkspaceId, Executor, Move($$"""{"targetGroupId":"{{GroupId}}"}"""),
            CancellationToken.None, BoardId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GroupOnAnotherBoard_IsRejectedByTheWorkContext()
    {
        var port = new Mock<IWorkActionPort>();
        port.Setup(p => p.EnsureBoardGroupAsync(BoardId, GroupId, It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("BoardGroup", GroupId));
        var authority = new AutomationExecutorAuthority(port.Object);

        var act = () => authority.EnsureCanExecuteAsync(
            AccountId, WorkspaceId, Executor, Move($$"""{"targetGroupId":"{{GroupId}}"}"""),
            CancellationToken.None, BoardId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task BoardRule_WithAnyAction_StillChecksTheBoard()
    {
        var port = new Mock<IWorkActionPort>();
        var authority = new AutomationExecutorAuthority(port.Object);

        await authority.EnsureCanExecuteAsync(
            AccountId, WorkspaceId, Executor,
            AutomationActionDefinition.Create("SendEmail", $$"""{"userId":"{{Guid.CreateVersion7()}}","subject":"x"}"""),
            CancellationToken.None, BoardId);

        port.Verify(p => p.EnsureBoardGroupAsync(BoardId, null, It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ItemOnAnotherBoard_FailsTheExecution_AndNeverMoves()
    {
        using var context = new TestAutomationDbContext(new DbContextOptionsBuilder<TestAutomationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
        var rule = AutomationRule.Create(
            AccountId, WorkspaceId, "rule",
            AutomationConfiguration.Create(
                AutomationTriggerDefinition.Create("ItemMovedToGroup", $$"""{"groupId":"{{GroupId}}"}"""),
                Move($$"""{"targetGroupId":"{{GroupId}}"}""")),
            Executor, T0, BoardId);
        rule.Enable(Executor, T0);
        context.AutomationRules.Add(rule);
        var execution = AutomationExecution.Create(AccountId, WorkspaceId, rule.Id, Guid.NewGuid(), T0);
        context.AutomationExecutions.Add(execution);
        await context.SaveChangesAsync();

        var port = new Mock<IWorkActionPort>();
        port.Setup(p => p.EnsureItemOnBoardAsync(ItemId, BoardId, It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotFoundException("BoardItem", ItemId));
        var useCase = new AutomationMoveItemUseCase(
            context, port.Object, new Clock(T0.AddMinutes(5)), new Mock<IOrderingConflictClassifier>().Object);

        var complete = await useCase.ExecuteAsync(
            new AutomationMoveItemRequestedV1(Guid.CreateVersion7(), execution.Id, rule.Id, AccountId, WorkspaceId,
                Executor, T0, Guid.NewGuid(), null, ItemId),
            CancellationToken.None);

        complete.Should().BeTrue();
        (await context.AutomationExecutions.SingleAsync()).Status.Should().Be(AutomationExecutionStatus.Failed);
        port.Verify(p => p.MoveItemAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
