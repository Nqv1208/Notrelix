using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Events.Automation;
using Notrelix.Application.Features.Automation.Executions.Services;
using Notrelix.Application.Features.Automation.Events;
using Notrelix.Application.Features.Automation.Ports.WorkManagement;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Common;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Events;
using Notrelix.Infrastructure.Messaging;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Automation;

/// <summary>
/// TAC-AI-002/003 + AI-FLOW-02/03 — the runtime trigger matrix over real
/// PostgreSQL: each runtime-reachable Work fact (member-assigned, moved,
/// created) drives the same evaluator path — one execution per (rule,
/// source event), the intent routing follows the rule's action type, and
/// duplicate deliveries of the same source event never duplicate executions.
/// The MoveItem intent is executed by the production Application executor
/// through the Automation→Work port, proving success, terminal business
/// rejection, and retryable technical failure progressions (TAC-AI-011A).
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class AutomationTriggerMatrixIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public AutomationTriggerMatrixIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed record TriggerGraph(
        Guid AccountId,
        Guid WorkspaceId,
        Guid RuleId,
        Guid ItemId);

    private async Task<TriggerGraph> SeedRuleAsync(
        string triggerType,
        string actionType,
        string? actionConfigPattern = null,
        string? triggerConfigPattern = null)
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create(accountId, ownerId, "Trigger WS", $"trg-{Guid.NewGuid():N}", Now);
        var member = WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now);
        var board = Board.Create(accountId, workspace.Id, ownerId, "Board", null, Now);
        var group = BoardGroup.Create(accountId, workspace.Id, board.Id, "Todo", Color.Create("#808080"), FractionalIndex.Initial(), ownerId, Now);
        var item = BoardItem.CreateRoot(accountId, workspace.Id, board.Id, group.Id, "Task", FractionalIndex.Initial(), ownerId, Now);

        var actionConfig = actionConfigPattern?
            .Replace("{itemId}", item.Id.ToString())
            .Replace("{groupId}", group.Id.ToString());
        var triggerConfig = triggerConfigPattern?
            .Replace("{itemId}", item.Id.ToString())
            .Replace("{groupId}", group.Id.ToString());
        var config = AutomationConfiguration.Create(
            AutomationTriggerDefinition.Create(triggerType, triggerConfig),
            AutomationActionDefinition.Create(actionType, actionConfig));
        var rule = AutomationRule.Create(Guid.NewGuid(), workspace.Id, $"Rule {triggerType}", config, ownerId, Now);
        rule.Enable(ownerId, Now);

        var user = Domain.Identity.Users.User.Create($"trg-{Guid.NewGuid():N}@example.com", "Trigger User", "hashed", Now, true);
        var account = Domain.Accounts.Accounts.Account.Create("Trigger Account", $"trg-{Guid.NewGuid():N}", Domain.Accounts.Accounts.AccountType.Team, ownerId, Now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Users.Add(user);
        seed.Accounts.Add(account);
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(member);
        seed.Boards.Add(board);
        seed.BoardGroups.Add(group);
        seed.BoardItems.Add(item);
        seed.AutomationRules.Add(rule);
        await seed.SaveChangesAsync();

        return new TriggerGraph(account.Id, workspace.Id, rule.Id, item.Id);
    }

    /// <summary>
    /// Runs the evaluator and returns the number of durable dispatch intents
    /// staged. The SaveChanges interceptor consumes the collector's pending
    /// events, so the count comes from committed outbox rows — never from the
    /// collector itself.
    /// </summary>
    private async Task<int> EvaluateAsync(
        TriggerGraph graph,
        AutomationTriggerContext trigger)
    {
        var intentsBefore = await CountIntentsAsync(graph.WorkspaceId, "*");
        var collector = new IntegrationEventCollector();
        await using var context = _db.CreateContext(SystemTenant(), CreateOutboxInterceptor(collector));
        var evaluator = new N8nAutomationRuleEvaluator(context, collector, new FixedClock(Now));
        await evaluator.ExecuteAsync(trigger, CancellationToken.None);
        await context.SaveChangesAsync();
        return await CountIntentsAsync(graph.WorkspaceId, "*") - intentsBefore;
    }

    private static DomainEventInterceptor CreateOutboxInterceptor(
        IntegrationEventCollector collector)
    {
        return new DomainEventInterceptor(
            new FixedClock(Now),
            new EventTypeRegistry(),
            ClassificationPolicy.CreateBuilder().Build(),
            DeliveryPolicy.CreateBuilder().Build(),
            new CompositeIntegrationEventMapper(
                new ServiceCollection().BuildServiceProvider()),
            collector);
    }

    [Fact]
    public async Task MemberAssignedTrigger_StillStagesN8nIntent_WithOneExecutionPerSourceEvent()
    {
        var graph = await SeedRuleAsync("ItemAssigned", "Webhook", """{"webhookPath":"notrelix-card-assigned"}""");
        var sourceEventId = Guid.NewGuid();

        var trigger = new AutomationTriggerContext(
            graph.AccountId, graph.WorkspaceId, "ItemAssigned",
            sourceEventId, Guid.NewGuid(), Guid.NewGuid(), null, Now);

        (await EvaluateAsync(graph, trigger)).Should().Be(1);
        (await CountExecutionsForRuleAsync(graph.RuleId)).Should().Be(1);
        (await CountIntentsAsync(graph.WorkspaceId, "automation.n8n-dispatch-requested")).Should().Be(1);

        // Duplicate delivery of the SAME source event: the evaluator dedups.
        (await EvaluateAsync(graph, trigger)).Should().Be(0);
        (await CountExecutionsForRuleAsync(graph.RuleId)).Should().Be(1,
            "a duplicate source event must never create a second execution");
    }

    [Fact]
    public async Task ItemMovedTrigger_StagesMoveItemIntent()
    {
        var graph = await SeedRuleAsync(
            "ItemMovedToGroup", "MoveItem",
            """{"itemId":"{itemId}","targetGroupId":"{groupId}"}""",
            """{"groupId":"{groupId}"}""");
        var sourceEventId = Guid.NewGuid();

        var trigger = new AutomationTriggerContext(
            graph.AccountId, graph.WorkspaceId, "ItemMovedToGroup",
            sourceEventId, Guid.NewGuid(), Guid.NewGuid(), null, Now);

        (await EvaluateAsync(graph, trigger)).Should().Be(1);
        (await CountExecutionsForRuleAsync(graph.RuleId)).Should().Be(1);
        (await CountIntentsAsync(graph.WorkspaceId, "automation.move-item-requested")).Should().Be(1,
            "MoveItem rules stage the target-action intent, not the n8n dispatch");
    }

    [Fact]
    public async Task ItemCreatedTrigger_StagesN8nIntent()
    {
        var graph = await SeedRuleAsync("ItemCreated", "Webhook", """{"webhookPath":"notrelix-card-created"}""");
        var sourceEventId = Guid.NewGuid();

        var trigger = new AutomationTriggerContext(
            graph.AccountId, graph.WorkspaceId, "ItemCreated",
            sourceEventId, Guid.NewGuid(), Guid.NewGuid(), null, Now);

        (await EvaluateAsync(graph, trigger)).Should().Be(1);
        (await CountExecutionsForRuleAsync(graph.RuleId)).Should().Be(1);
        (await CountIntentsAsync(graph.WorkspaceId, "automation.n8n-dispatch-requested")).Should().Be(1);
    }

    [Fact]
    public async Task MoveItemExecutor_Success_ProducesTargetMove_AndTerminalExecution()
    {
        var targetGroupId = Guid.NewGuid();
        var graph = await SeedRuleAsync(
            "ItemMovedToGroup", "MoveItem",
            """{"itemId":"{itemId}","targetGroupId":"{groupId}"}""",
            """{"groupId":"{groupId}"}""");
        var execution = await SeedExecutionAsync(graph);
        var movedIds = new List<(Guid ItemId, Guid GroupId, Guid OperationId)>();

        var port = new Mock<IWorkActionPort>();
        port.Setup(p => p.MoveItemAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .Returns<Guid, Guid, Guid, AutomationPrincipal, CancellationToken>((itemId, groupId, operationId, _, _) =>
            {
                movedIds.Add((itemId, groupId, operationId));
                return Task.FromResult(new WorkActionResult(itemId, groupId, "a1"));
            });

        var message = NewMoveItemMessage(execution, actorUserId: Guid.NewGuid());
        var complete = await ExecuteMoveItemAsync(graph, execution, port.Object, message);

        complete.Should().BeTrue("a successful target move is a terminal success");
        movedIds.Should().ContainSingle().Which.OperationId.Should().Be(execution.Id,
            "the execution id is the target OperationId for dedup");
        movedIds.Should().ContainSingle().Which.GroupId.Should().NotBeEmpty();

        var stored = await LoadExecutionAsync(execution.Id);
        stored.Status.Should().Be(AutomationExecutionStatus.Succeeded);
    }

    [Fact]
    public async Task MoveItemExecutor_InvalidConfiguration_TerminalFailure_WithoutTargetInvocation()
    {
        // Rule passes rule-level validation (targetGroupId present) but the
        // executor cannot parse the itemId — an execution-configuration defect
        // that must terminate the execution without invoking the target.
        var graph = await SeedRuleAsync(
            "ItemMovedToGroup", "MoveItem",
            """{"itemId":"not-a-guid","targetGroupId":"{groupId}"}""",
            """{"groupId":"{groupId}"}""");
        var execution = await SeedExecutionAsync(graph);

        var port = new Mock<IWorkActionPort>();
        var message = NewMoveItemMessage(execution, actorUserId: Guid.NewGuid());

        var complete = await ExecuteMoveItemAsync(graph, execution, port.Object, message);

        complete.Should().BeTrue("an invalid configuration is a terminal failure, not a retry");
        port.Verify(p => p.MoveItemAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()), Times.Never,
            "the target action must not be invoked for an unparseable configuration");

        var stored = await LoadExecutionAsync(execution.Id);
        stored.Status.Should().Be(AutomationExecutionStatus.Failed);
    }

    [Fact]
    public async Task MoveItemExecutor_TargetBusinessRejection_TerminalFailure()
    {
        var targetGroupId = Guid.NewGuid();
        var graph = await SeedRuleAsync(
            "ItemMovedToGroup", "MoveItem",
            """{"itemId":"{itemId}","targetGroupId":"{groupId}"}""",
            """{"groupId":"{groupId}"}""");
        var execution = await SeedExecutionAsync(graph);

        var port = new Mock<IWorkActionPort>();
        port.Setup(p => p.MoveItemAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Notrelix.Application.Common.Exceptions.BusinessRuleException(
                "target group missing"));

        var message = NewMoveItemMessage(execution, actorUserId: Guid.NewGuid());
        var complete = await ExecuteMoveItemAsync(graph, execution, port.Object, message);

        complete.Should().BeTrue("a business rejection is a terminal failure");
        var stored = await LoadExecutionAsync(execution.Id);
        stored.Status.Should().Be(AutomationExecutionStatus.Failed);
    }

    [Fact]
    public async Task MoveItemExecutor_TechnicalFailure_StayedRetryable_WithSameExecutionId()
    {
        var targetGroupId = Guid.NewGuid();
        var graph = await SeedRuleAsync(
            "ItemMovedToGroup", "MoveItem",
            """{"itemId":"{itemId}","targetGroupId":"{groupId}"}""",
            """{"groupId":"{groupId}"}""");
        var execution = await SeedExecutionAsync(graph);

        var port = new Mock<IWorkActionPort>();
        port.Setup(p => p.MoveItemAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("network unreachable"));

        var message = NewMoveItemMessage(execution, actorUserId: Guid.NewGuid());
        var consume = () => ExecuteMoveItemAsync(graph, execution, port.Object, message);
        await consume.Should().ThrowAsync<Notrelix.Infrastructure.Messaging.Consumers.Automation.AutomationMoveItemRetryableException>(
            "a technical failure surfaces to the broker retry contract, not as success");

        var afterFirst = await LoadExecutionAsync(execution.Id);
        afterFirst.Status.Should().Be(AutomationExecutionStatus.Queued,
            "a retryable failure records evidence and re-queues the execution");
        afterFirst.AttemptCount.Should().Be(1);

        var secondMessage = message;
        var movedIds = new List<(Guid ItemId, Guid GroupId, Guid OperationId)>();
        var succeedingPort = new Mock<IWorkActionPort>();
        succeedingPort.Setup(p => p.MoveItemAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<AutomationPrincipal>(), It.IsAny<CancellationToken>()))
            .Returns<Guid, Guid, Guid, AutomationPrincipal, CancellationToken>((itemId, groupId, operationId, _, _) =>
            {
                movedIds.Add((itemId, groupId, operationId));
                return Task.FromResult(new WorkActionResult(itemId, groupId, "a1"));
            });

        var retryComplete = await ExecuteMoveItemAsync(graph, execution, succeedingPort.Object, secondMessage);
        retryComplete.Should().BeTrue();
        movedIds.Should().ContainSingle().Which.OperationId.Should().Be(execution.Id,
            "redelivery reuses the identical execution identity");

        var stored = await LoadExecutionAsync(execution.Id);
        stored.Status.Should().Be(AutomationExecutionStatus.Succeeded);
    }

    // --- helpers --------------------------------------------------------------

    private static AutomationMoveItemRequestedV1 NewMoveItemMessage(
        AutomationExecution execution, Guid actorUserId) =>
        new(Guid.CreateVersion7(), execution.Id, execution.RuleId,
            execution.AccountId, execution.WorkspaceId, actorUserId,
            DateTimeOffset.UtcNow, Guid.NewGuid(), null);

    private async Task<AutomationExecution> SeedExecutionAsync(TriggerGraph graph)
    {
        var execution = AutomationExecution.Create(
            graph.AccountId, graph.WorkspaceId, graph.RuleId, Guid.NewGuid(), Now);
        await using var seed = _db.CreateContext(SystemTenant());
        seed.AutomationExecutions.Add(execution);
        await seed.SaveChangesAsync();
        ((IHasDomainEvents)execution).ClearDomainEvents();
        return execution;
    }

    private async Task<bool> ExecuteMoveItemAsync(
        TriggerGraph graph,
        AutomationExecution execution,
        IWorkActionPort port,
        AutomationMoveItemRequestedV1 message)
    {
        await using var context = _db.CreateContext(SystemTenant());
        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(Now);

        var useCase = new AutomationMoveItemUseCase(context, port, clockMock.Object);

        var consumeContext = new Mock<ConsumeContext<AutomationMoveItemRequestedV1>>();
        consumeContext.SetupGet(c => c.Message).Returns(message);
        consumeContext.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        var consumer = new Notrelix.Infrastructure.Messaging.Consumers.Automation.AutomationMoveItemDispatchConsumer(
            useCase,
            NullLogger<Notrelix.Infrastructure.Messaging.Consumers.Automation.AutomationMoveItemDispatchConsumer>.Instance,
            new PipelineMetrics());

        await consumer.Consume(consumeContext.Object);
        await context.SaveChangesAsync();
        return await IsTerminalAsync(execution.Id);
    }

    private async Task<bool> IsTerminalAsync(Guid executionId)
    {
        var stored = await LoadExecutionAsync(executionId);
        return stored.Status
            is AutomationExecutionStatus.Succeeded
            or AutomationExecutionStatus.Failed;
    }

    private async Task<int> CountExecutionsForRuleAsync(Guid ruleId)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        return await probe.AutomationExecutions.IgnoreQueryFilters()
            .CountAsync(e => e.RuleId == ruleId);
    }

    private async Task<int> CountIntentsAsync(Guid workspaceId, string messageName)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        var query = probe.Set<MessagingOutboxMessage>()
            .IgnoreQueryFilters()
            .Where(m => m.WorkspaceId == workspaceId);
        return messageName == "*"
            ? await query.CountAsync()
            : await query.CountAsync(m => m.MessageName == messageName);
    }

    private async Task<AutomationExecution> LoadExecutionAsync(Guid id)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        return await probe.AutomationExecutions.IgnoreQueryFilters().SingleAsync(x => x.Id == id);
    }

    private FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private sealed class FixedClock(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now;
    }
}
