using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Events.Automation;
using Notrelix.Application.Features.Automation.Executions.Queries.GetAutomationExecutions;
using Notrelix.Application.Features.Automation.Executions.Services;
using Notrelix.Application.Features.Automation.Ports;
using Notrelix.Application.Features.Automation.Ports.WorkManagement;
using Notrelix.Application.Features.Automation.Rules.Services;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;

namespace Notrelix.Application.Tests.Features.Automation.Executions;

/// <summary>
/// A rule runs one trigger, optional AND conditions and up to five ordered actions on the item
/// that triggered it; each action is idempotent per execution and place, an edit is a new version
/// that queued executions do not see, and a deleted rule's in-flight executions still finish.
/// </summary>
public class AutomationMultiActionTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.CreateVersion7();
    private static readonly Guid WorkspaceId = Guid.CreateVersion7();
    private static readonly Guid BoardId = Guid.CreateVersion7();
    private static readonly Guid Executor = Guid.CreateVersion7();
    private static readonly Guid ItemId = Guid.CreateVersion7();
    private static readonly Guid Option = Guid.CreateVersion7();
    private static readonly Guid Person = Guid.CreateVersion7();
    private static readonly Guid Field = Guid.CreateVersion7();

    private sealed class Clock(DateTimeOffset now) : Notrelix.Application.Common.Time.IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }

    private static TestAutomationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<TestAutomationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static AutomationConfiguration Config(params (string Type, string Json)[] actions) =>
        AutomationRuleAssembler.Assemble(
            "ItemCreated", $$"""{}""", BoardId,
            actions.Select(a => new RuleActionInput(a.Type, a.Json)).ToList(), null);

    private static readonly (string, string) SetStatus = ("ChangeStatus", $$"""{"optionId":"{{Option}}"}""");
    private static readonly (string, string) Assign = ("AssignPerson", $$"""{"userId":"{{Person}}"}""");
    private static readonly (string, string) Notify = ("NotifyMember", $$"""{"userId":"{{Person}}","message":"Moved"}""");

    private static async Task<(AutomationRule rule, AutomationExecution execution)> StageAsync(
        TestAutomationDbContext context, AutomationConfiguration configuration, bool snapshot = true)
    {
        var rule = AutomationRule.Create(AccountId, WorkspaceId, "rule", configuration, Executor, T0, BoardId);
        rule.Enable(Executor, T0);
        context.AutomationRules.Add(rule);
        var execution = AutomationExecution.Create(
            AccountId, WorkspaceId, rule.Id, Guid.NewGuid(), T0, null, 0,
            snapshot ? rule.Version : null,
            snapshot ? AutomationRuleSnapshot.From(rule).Serialize() : null);
        context.AutomationExecutions.Add(execution);
        await context.SaveChangesAsync();
        return (rule, execution);
    }

    private static AutomationMoveItemUseCase UseCase(
        TestAutomationDbContext context, Mock<IWorkActionPort> port, Mock<IAutomationMessagePort>? messages = null) =>
        new(context, port.Object, new Clock(T0.AddMinutes(5)), new Mock<IOrderingConflictClassifier>().Object,
            null, (messages ?? new Mock<IAutomationMessagePort>()).Object);

    private static AutomationMoveItemRequestedV1 Message(AutomationExecution execution, AutomationRule rule) =>
        new(Guid.CreateVersion7(), execution.Id, rule.Id, AccountId, WorkspaceId, Executor, T0, Guid.NewGuid(), null, ItemId);

    [Fact]
    public async Task RunsEveryActionInOrder_OnTheTriggeringItem_AndRecordsEachStep()
    {
        using var context = NewContext();
        var (rule, execution) = await StageAsync(context, Config(SetStatus, Assign, Notify));
        var calls = new List<string>();
        var port = new Mock<IWorkActionPort>();
        port.Setup(p => p.ChangeStatusAsync(ItemId, Option, It.IsAny<Guid>(), It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("status")).Returns(Task.CompletedTask);
        port.Setup(p => p.AssignMemberAsync(ItemId, Person, It.IsAny<Guid>(), It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("assign")).Returns(Task.CompletedTask);
        port.Setup(p => p.GetItemFactsAsync(ItemId, BoardId, It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkActionItemFacts(Guid.NewGuid(), null, "Task 1", new Dictionary<Guid, string>()));
        var messages = new Mock<IAutomationMessagePort>();
        messages.Setup(m => m.NotifyMemberAsync(AccountId, WorkspaceId, Person, ItemId, Executor, It.IsAny<Guid>(), "Moved", "Task 1", It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("notify")).Returns(Task.CompletedTask);

        var complete = await UseCase(context, port, messages).ExecuteAsync(Message(execution, rule), default);

        complete.Should().BeTrue();
        calls.Should().Equal("status", "assign", "notify");
        var saved = await context.AutomationExecutions.SingleAsync();
        saved.Status.Should().Be(AutomationExecutionStatus.Succeeded);
        AutomationExecutionProgress.Read(saved.LastResponse)!.Select(s => (s.Index, s.Status))
            .Should().Equal((0, "Succeeded"), (1, "Succeeded"), (2, "Succeeded"));
    }

    [Fact]
    public async Task EachActionGetsItsOwnOperationId_TheFirstKeepsTheExecutionId()
    {
        using var context = NewContext();
        var (rule, execution) = await StageAsync(context, Config(SetStatus, Assign));
        var ids = new List<Guid>();
        var port = new Mock<IWorkActionPort>();
        port.Setup(p => p.ChangeStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, Guid, AutomationPrincipal, CancellationToken>((_, _, op, _, _) => ids.Add(op)).Returns(Task.CompletedTask);
        port.Setup(p => p.AssignMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, Guid, AutomationPrincipal, CancellationToken>((_, _, op, _, _) => ids.Add(op)).Returns(Task.CompletedTask);

        await UseCase(context, port).ExecuteAsync(Message(execution, rule), default);

        ids[0].Should().Be(execution.Id);
        ids[1].Should().NotBe(execution.Id).And.Be(AutomationMoveItemUseCase.StepOperationId(execution.Id, 1));
    }

    [Fact]
    public async Task ARejectedAction_FailsTheExecution_AndLaterActionsDoNotRun()
    {
        using var context = NewContext();
        var (rule, execution) = await StageAsync(context, Config(SetStatus, Assign));
        var port = new Mock<IWorkActionPort>();
        port.Setup(p => p.ChangeStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Notrelix.Domain.Common.Exceptions.BusinessRuleException("x", "rejected"));

        var complete = await UseCase(context, port).ExecuteAsync(Message(execution, rule), default);

        complete.Should().BeTrue();
        var saved = await context.AutomationExecutions.SingleAsync();
        saved.Status.Should().Be(AutomationExecutionStatus.Failed);
        saved.Error.Should().StartWith("Action 1 (ChangeStatus)");
        port.Verify(p => p.AssignMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ARedelivery_ResumesAfterTheActionsThatAlreadySucceeded()
    {
        using var context = NewContext();
        var (rule, execution) = await StageAsync(context, Config(SetStatus, Assign));
        var port = new Mock<IWorkActionPort>();
        port.Setup(p => p.ChangeStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        port.Setup(p => p.AssignMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("transient"));

        var first = await UseCase(context, port).ExecuteAsync(Message(execution, rule), default);
        first.Should().BeFalse("a technical failure is retried");

        port.Setup(p => p.AssignMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var second = await UseCase(context, port).ExecuteAsync(Message(execution, rule), default);

        second.Should().BeTrue();
        port.Verify(p => p.ChangeStatusAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()), Times.Once);
        (await context.AutomationExecutions.SingleAsync()).Status.Should().Be(AutomationExecutionStatus.Succeeded);
    }

    [Fact]
    public async Task AnEditedRule_DoesNotChangeAnExecutionThatStartedOnTheOldVersion()
    {
        using var context = NewContext();
        var (rule, execution) = await StageAsync(context, Config(SetStatus));
        var oldVersion = rule.Version;
        rule.Revise("renamed", Config(Assign), Executor, T0.AddMinutes(1));
        await context.SaveChangesAsync();
        rule.Version.Should().BeGreaterThan(oldVersion);
        var port = new Mock<IWorkActionPort>();

        await UseCase(context, port).ExecuteAsync(Message(execution, rule), default);

        port.Verify(p => p.ChangeStatusAsync(ItemId, Option, It.IsAny<Guid>(), It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()), Times.Once);
        port.Verify(p => p.AssignMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()), Times.Never);
        (await context.AutomationExecutions.SingleAsync()).RuleVersion.Should().Be(oldVersion);
    }

    [Fact]
    public async Task ADeletedRule_StillFinishesItsInFlightExecution()
    {
        using var context = NewContext();
        var (rule, execution) = await StageAsync(context, Config(SetStatus));
        rule.Remove(Executor, T0.AddMinutes(1));
        await context.SaveChangesAsync();
        var port = new Mock<IWorkActionPort>();

        var complete = await UseCase(context, port).ExecuteAsync(Message(execution, rule), default);

        complete.Should().BeTrue();
        (await context.AutomationExecutions.SingleAsync()).Status.Should().Be(AutomationExecutionStatus.Succeeded);
    }

    [Fact]
    public async Task ADeletedRulesHistory_IsReadableFor90Days_ThenGone()
    {
        using var context = NewContext();
        var (rule, _) = await StageAsync(context, Config(SetStatus));
        rule.Remove(Executor, T0);
        await context.SaveChangesAsync();

        var within = new GetAutomationExecutionsQueryHandler(context, new Clock(T0.AddDays(89)));
        var after = new GetAutomationExecutionsQueryHandler(context, new Clock(T0.AddDays(91)));

        (await within.Handle(new GetAutomationExecutionsQuery(rule.Id), default)).Succeeded.Should().BeTrue();
        var gone = () => after.Handle(new GetAutomationExecutionsQuery(rule.Id), default);
        await gone.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ADeletedRule_CannotBeEnabledOrEdited()
    {
        using var context = NewContext();
        var (rule, _) = await StageAsync(context, Config(SetStatus));
        rule.Remove(Executor, T0);

        var enable = () => rule.Enable(Executor, T0);
        var revise = () => rule.Revise("x", Config(Assign), Executor, T0);

        enable.Should().Throw<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        revise.Should().Throw<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
    }

    [Fact]
    public async Task ADisabledRule_StopsItsQueuedExecution()
    {
        using var context = NewContext();
        var (rule, execution) = await StageAsync(context, Config(SetStatus));
        rule.Disable(Executor, T0.AddMinutes(1));
        await context.SaveChangesAsync();

        await UseCase(context, new Mock<IWorkActionPort>()).ExecuteAsync(Message(execution, rule), default);

        (await context.AutomationExecutions.SingleAsync()).Status.Should().Be(AutomationExecutionStatus.Failed);
    }

    [Fact]
    public void Assembler_RejectsMoreThanFiveActions_AndActionsWithoutAnExecutor()
    {
        var six = Enumerable.Repeat(SetStatus, 6).ToArray();
        var tooMany = () => Config(six);
        var noExecutor = () => Config(("CreateItem", $$"""{"targetGroupId":"{{Guid.NewGuid()}}"}"""));
        var integration = () => Config(("Webhook", """{"webhookPath":"hook"}"""));

        tooMany.Should().Throw<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        noExecutor.Should().Throw<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        integration.Should().Throw<Notrelix.Domain.Common.Exceptions.BusinessRuleException>("a board rule takes no integration action");
        Config(SetStatus, Assign, Notify).Actions.Should().HaveCount(3);
    }

    [Fact]
    public void Conditions_AreAnAndOfEqualsTests()
    {
        var group = Guid.NewGuid();
        var json = $$"""{"all":[{"type":"group","groupId":"{{group}}"},{"type":"status","optionId":"{{Option}}"},{"type":"field","fieldId":"{{Field}}","value":"Urgent"}]}""";
        AutomationConditions.TryParse(json, out var conditions).Should().BeTrue();
        var matching = new WorkActionItemFacts(group, Option, "t", new Dictionary<Guid, string> { [Field] = "\"Urgent\"" });

        AutomationConditions.Holds(conditions, matching).Should().BeTrue();
        AutomationConditions.Holds(conditions, matching with { GroupId = Guid.NewGuid() }).Should().BeFalse();
        AutomationConditions.Holds(conditions, matching with { StatusOptionId = null }).Should().BeFalse();
        AutomationConditions.Holds(conditions, matching with { FieldValues = new Dictionary<Guid, string>() }).Should().BeFalse();
        AutomationConditions.TryParse("""{"all":[{"type":"nope"}]}""", out _).Should().BeFalse();
    }
}
