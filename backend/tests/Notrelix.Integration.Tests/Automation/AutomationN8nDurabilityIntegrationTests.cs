using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Events.Automation;
using Notrelix.Application.Events.WorkManagement;
using Notrelix.Application.Features.Automation.Events;
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

using Notrelix.Infrastructure.Events;
using Notrelix.Infrastructure.Data.Interceptors;
using Notrelix.Infrastructure.Data.Messaging;
using Notrelix.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Infrastructure.Messaging.Consumers.Automation;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Automation;

/// <summary>
/// IA-TST-N8N-DB — durable automation/N8n acceptance on real PostgreSQL
/// (freeze file 03): execution + outbox intent commit atomically, duplicate
/// source triggers cannot create a second execution (database-enforced), the
/// broker consumer propagates the stable ExecutionId, and transient network
/// failures rethrow so the broker retries without losing durable state.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class AutomationN8nDurabilityIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public AutomationN8nDurabilityIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task AcceptedTrigger_ExecutionAndOutboxIntent_CommitAtomically()
    {
        var graph = await SeedRuleAsync();
        var integrationEvent = NewIntegrationEvent(graph.AccountId, graph.WorkspaceId);

        var executionsBefore = await CountExecutionsAsync();

        var collector = new IntegrationEventCollector();
        await using var context = _db.CreateContext(SystemTenant(), CreateOutboxInterceptor(collector));
        var evaluator = new N8nAutomationRuleEvaluator(
            context,
            collector,
            new FixedClock(Now));
        await evaluator.ExecuteAsync(integrationEvent, CancellationToken.None);
        await context.SaveChangesAsync();

        (await CountExecutionsAsync()).Should().Be(executionsBefore + 1);
        (await CountOutboxIntentsAsync(graph.WorkspaceId)).Should().Be(1,
            "the accepted trigger must stage exactly one durable dispatch intent");
    }

    [Fact]
    public async Task ForcedRollback_NeitherExecutionNorIntentCommits()
    {
        var graph = await SeedRuleAsync();
        var integrationEvent = NewIntegrationEvent(graph.AccountId, graph.WorkspaceId);
        var executionsBefore = await CountExecutionsAsync();

        var collector = new IntegrationEventCollector();
        await using var context = _db.CreateContext(SystemTenant(), CreateOutboxInterceptor(collector));
        await using var transaction =
            await context.Database.BeginTransactionAsync(CancellationToken.None);
        var evaluator = new N8nAutomationRuleEvaluator(
            context,
            collector,
            new FixedClock(Now));
        await evaluator.ExecuteAsync(integrationEvent, CancellationToken.None);
        await context.SaveChangesAsync(CancellationToken.None);
        await transaction.RollbackAsync(CancellationToken.None);

        (await CountExecutionsAsync()).Should().Be(executionsBefore,
            "a rolled-back trigger must not persist the execution");
        (await CountOutboxIntentsAsync(graph.WorkspaceId)).Should().Be(0,
            "a rolled-back trigger must not persist the dispatch intent");
    }

    [Fact]
    public async Task DuplicateSameSourceTrigger_DatabaseUniqueness_AllowsOnlyOneExecution()
    {
        var graph = await SeedRuleAsync();
        var integrationEvent = NewIntegrationEvent(graph.AccountId, graph.WorkspaceId);
        var executionsBefore = await CountExecutionsAsync();

        // First accepted delivery persists normally through the evaluator.
        var collector = new IntegrationEventCollector();
        await using (var context = _db.CreateContext(SystemTenant(), CreateOutboxInterceptor(collector)))
        {
            var evaluator = new N8nAutomationRuleEvaluator(
                context,
                collector,
                new FixedClock(Now));
            await evaluator.ExecuteAsync(integrationEvent, CancellationToken.None);
            await context.SaveChangesAsync(CancellationToken.None);
        }

        // A racing writer that bypasses the application-level existence check is
        // still rejected by ux_automation_executions_rule_trigger at the database.
        await using (var racyContext = _db.CreateContext(SystemTenant()))
        {
            var duplicate = AutomationExecution.Create(
                graph.AccountId, graph.WorkspaceId, graph.RuleId,
                integrationEvent.SourceEventId!.Value, Now.AddSeconds(1));
            racyContext.AutomationExecutions.Add(duplicate);

            var save = () => racyContext.SaveChangesAsync(CancellationToken.None);
            await save.Should().ThrowAsync<DbUpdateException>();
        }

        (await CountExecutionsAsync()).Should().Be(executionsBefore + 1);
    }

    [Fact]
    public async Task Consumer_PropagatesStableExecutionId_ToNetworkAdapter()
    {
        var graph = await SeedRuleAsync();
        var execution = await SeedExecutionAsync(graph);
        string? seenExecutionId = null;

        var adapter = new Mock<IN8nClient>();
        adapter.Setup(client => client.TriggerWebhookAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((_, payload, _) =>
            {
                seenExecutionId = payload;
                return Task.FromResult(new N8nTriggerResult(true, 200, null, null));
            });

        var message = NewDispatchMessage(execution);
        await InvokeConsumerAsync(graph, execution, adapter.Object, message);

        seenExecutionId.Should().NotBeNull();
        seenExecutionId.Should().Contain(execution.Id.ToString(),
            "ExecutionId is the stable external idempotency/correlation identity");

        var stored = await LoadExecutionAsync(execution.Id);
        stored.Status.Should().Be(AutomationExecutionStatus.Succeeded);
    }

    [Fact]
    public async Task RetryableNetworkFailure_Rethrows_AndSecondAttemptSucceedsWithSameExecutionId()
    {
        var graph = await SeedRuleAsync();
        var execution = await SeedExecutionAsync(graph);
        var payloads = new List<string>();

        var failingAdapter = new Mock<IN8nClient>();
        failingAdapter.Setup(client => client.TriggerWebhookAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("n8n unreachable"));

        var consumeFirst = () => InvokeConsumerAsync(graph, execution, failingAdapter.Object, NewDispatchMessage(execution));
        await consumeFirst.Should().ThrowAsync<HttpRequestException>(
            "transient failures must surface to the broker retry contract, not be swallowed as success");

        var succeededAdapter = new Mock<IN8nClient>();
        succeededAdapter.Setup(client => client.TriggerWebhookAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((_, payload, _) =>
            {
                payloads.Add(payload);
                return Task.FromResult(new N8nTriggerResult(true, 200, null, null));
            });

        await InvokeConsumerAsync(graph, execution, succeededAdapter.Object, NewDispatchMessage(execution));

        payloads.Should().ContainSingle()
            .Which.Should().Contain(execution.Id.ToString(),
                "redelivery of the same durable execution reuses the identical ExecutionId");

        var stored = await LoadExecutionAsync(execution.Id);
        stored.Status.Should().Be(AutomationExecutionStatus.Succeeded);
    }

    [Fact]
    public async Task FailingDispatch_DoesNotBlockIndependentDispatch()
    {
        var firstGraph = await SeedRuleAsync();
        var secondGraph = await SeedRuleAsync();
        var firstExecution = await SeedExecutionAsync(firstGraph);
        var secondExecution = await SeedExecutionAsync(secondGraph);

        var throwingAdapter = new Mock<IN8nClient>();
        throwingAdapter.Setup(client => client.TriggerWebhookAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("n8n unreachable"));

        var consumeFailing = () => InvokeConsumerAsync(firstGraph, firstExecution, throwingAdapter.Object, NewDispatchMessage(firstExecution));
        await consumeFailing.Should().ThrowAsync<HttpRequestException>();

        var succeedingAdapter = new Mock<IN8nClient>();
        succeedingAdapter.Setup(client => client.TriggerWebhookAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new N8nTriggerResult(true, 200, null, null));

        await InvokeConsumerAsync(secondGraph, secondExecution, succeedingAdapter.Object, NewDispatchMessage(secondExecution));

        (await LoadExecutionAsync(secondExecution.Id)).Status.Should().Be(AutomationExecutionStatus.Succeeded,
            "one failing dispatch must not head-of-line block unrelated dispatches");
    }

    // --- helpers --------------------------------------------------------------

    private sealed record RuleGraph(Guid AccountId, Guid WorkspaceId, Guid RuleId);

    private async Task<RuleGraph> SeedRuleAsync()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create(accountId, ownerId, "Automation WS", $"n8n-{Guid.NewGuid():N}", Now);
        var member = WorkspaceMember.Create(Guid.NewGuid(), workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now);
        var board = Board.Create(accountId, workspace.Id, ownerId, "Board", null, Now);
        var group = BoardGroup.Create(accountId, workspace.Id, board.Id, "Todo", Color.Create("#808080"), FractionalIndex.Initial(), ownerId, Now);
        var item = BoardItem.CreateRoot(accountId, workspace.Id, board.Id, group.Id, "Task", FractionalIndex.Initial(), ownerId, Now);

        var config = AutomationConfiguration.Create(
            AutomationTriggerDefinition.Create("ItemAssigned"),
            AutomationActionDefinition.Create("Webhook", """{"webhookPath":"notrelix-card-assigned"}"""));
        var rule = AutomationRule.Create(Guid.NewGuid(), workspace.Id, "Card assigned alert", config, ownerId, Now);
        rule.Enable(ownerId, Now);

        var user = Domain.Identity.Users.User.Create($"n8n-{Guid.NewGuid():N}@example.com", "N8N User", "hashed", Now, true);
        var account = Domain.Accounts.Accounts.Account.Create("N8N Account", $"n8n-{Guid.NewGuid():N}", Domain.Accounts.Accounts.AccountType.Team, ownerId, Now);

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

        return new RuleGraph(account.Id, workspace.Id, rule.Id);
    }

    private async Task<AutomationExecution> SeedExecutionAsync(RuleGraph graph)
    {
        var execution = AutomationExecution.Create(
            graph.AccountId, graph.WorkspaceId, graph.RuleId, Guid.NewGuid(), Now);
        await using var seed = _db.CreateContext(SystemTenant());
        seed.AutomationExecutions.Add(execution);
        await seed.SaveChangesAsync();
        ((IHasDomainEvents)execution).ClearDomainEvents();
        return execution;
    }

    private static BoardItemMemberAssignedIntegrationEvent NewIntegrationEvent(
        Guid accountId, Guid workspaceId) =>
        new(Guid.CreateVersion7(), accountId, workspaceId, Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            SourceEventId: Guid.NewGuid(),
            OccurredAt: Now);

    private static N8nDispatchRequestedV1 NewDispatchMessage(AutomationExecution execution) =>
        new(Guid.CreateVersion7(), execution.Id, execution.RuleId,
            execution.AccountId, execution.WorkspaceId, DateTimeOffset.UtcNow,
            Guid.NewGuid(), execution.TriggerId, null);

    private async Task InvokeConsumerAsync(
        RuleGraph graph,
        AutomationExecution execution,
        IN8nClient adapter,
        N8nDispatchRequestedV1 message)
    {
        await using var context = _db.CreateContext(SystemTenant());
        var trackedExecution = await context.AutomationExecutions
            .SingleAsync(x => x.Id == execution.Id);
        ((IHasDomainEvents)trackedExecution).ClearDomainEvents();

        var consumer = new N8nDispatchConsumer(
            context,
            adapter,
            NullLogger<N8nDispatchConsumer>.Instance,
            new PipelineMetrics());

        var consumeContext = new Mock<ConsumeContext<N8nDispatchRequestedV1>>();
        consumeContext.SetupGet(c => c.Message).Returns(message);
        consumeContext.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(consumeContext.Object);
        await context.SaveChangesAsync();
    }

    private DomainEventInterceptor CreateOutboxInterceptor(IntegrationEventCollector collector)
    {
        var catalog = IntegrationEventCatalog.FromAppDomain();
        return new DomainEventInterceptor(
            new FixedClock(Now),
            new EventTypeRegistry(),
            ClassificationPolicy.CreateBuilder().Build(),
            DeliveryPolicy.CreateBuilder().Build(),
            new CompositeIntegrationEventMapper(
                new ServiceCollection()
                    .AddScoped<IIntegrationEventMapper, Notrelix.Application.EventMappers.WorkManagement.BoardItemMemberAssignedEventMapper>()
                    .BuildServiceProvider()),
            collector);
    }

    private async Task<int> CountExecutionsAsync()
    {
        await using var probe = _db.CreateContext(SystemTenant());
        return await probe.AutomationExecutions.IgnoreQueryFilters().CountAsync();
    }

    private async Task<int> CountOutboxIntentsAsync(Guid workspaceId)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        return await probe.Set<MessagingOutboxMessage>()
            .IgnoreQueryFilters()
            .CountAsync(message => message.WorkspaceId == workspaceId
                && message.MessageName == "automation.n8n-dispatch-requested");
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
