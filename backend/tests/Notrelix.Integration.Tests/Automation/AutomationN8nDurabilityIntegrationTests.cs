using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Events.Automation;
using Notrelix.Application.Events.WorkManagement;
using Notrelix.Application.Features.Automation.Executions.Services;
using Notrelix.Application.Features.Integrations.N8n.Providers;
using Notrelix.Application.Features.Integrations.N8n.Services;
using Notrelix.Application.Features.Integrations.Public.Commands;
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
                return Task.FromResult(new N8nWebhookDispatchResult(N8nWebhookOutcome.Succeeded, null));
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
            .ReturnsAsync(new N8nWebhookDispatchResult(
                N8nWebhookOutcome.RetryableFailure, "n8n webhook call failed: n8n unreachable"));

        var consumeFirst = () => InvokeConsumerAsync(graph, execution, failingAdapter.Object, NewDispatchMessage(execution));
        await consumeFirst.Should().ThrowAsync<N8nDispatchRetryableException>(
            "retryable provider failures must surface to the broker retry contract, not be swallowed as success");

        var attemptAfterFirstFailure = await LoadExecutionAsync(execution.Id);
        attemptAfterFirstFailure.Status.Should().Be(AutomationExecutionStatus.Queued,
            "a retryable failure records evidence and re-queues the execution for another Automation attempt");
        attemptAfterFirstFailure.AttemptCount.Should().Be(1);

        var succeededAdapter = new Mock<IN8nClient>();
        succeededAdapter.Setup(client => client.TriggerWebhookAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((_, payload, _) =>
            {
                payloads.Add(payload);
                return Task.FromResult(new N8nWebhookDispatchResult(N8nWebhookOutcome.Succeeded, null));
            });

        await InvokeConsumerAsync(graph, execution, succeededAdapter.Object, NewDispatchMessage(execution));

        payloads.Should().ContainSingle()
            .Which.Should().Contain(execution.Id.ToString(),
                "redelivery of the same durable execution reuses the identical ExecutionId");

        var stored = await LoadExecutionAsync(execution.Id);
        stored.Status.Should().Be(AutomationExecutionStatus.Succeeded);
    }

    [Fact]
    public async Task ProviderBusinessRejection_TerminalFailure_SettlesExecutionWithoutRedelivery()
    {
        var graph = await SeedRuleAsync();
        var execution = await SeedExecutionAsync(graph);
        var attempts = 0;

        var rejectingAdapter = new Mock<IN8nClient>();
        rejectingAdapter.Setup(client => client.TriggerWebhookAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((_, __, ___) =>
            {
                attempts++;
                return Task.FromResult(new N8nWebhookDispatchResult(
                    N8nWebhookOutcome.TerminalFailure, "n8n rejected the payload"));
            });

        var consume = () => InvokeConsumerAsync(graph, execution, rejectingAdapter.Object, NewDispatchMessage(execution));

        await consume.Should().NotThrowAsync(
            "a business rejection is terminal: the consumer must not signal the delivery mechanism to redeliver");

        attempts.Should().Be(1, "the consumer delegates to exactly one provider attempt and never retries internally");
        (await LoadExecutionAsync(execution.Id)).Status.Should().Be(AutomationExecutionStatus.Failed,
            "a provider business rejection settles the execution as a terminal failure");
    }

    [Fact]
    public async Task ProviderUnknownOutcome_SettlesExecutionAsFailed_WithoutRedelivery()
    {
        var graph = await SeedRuleAsync();
        var execution = await SeedExecutionAsync(graph);
        var attempts = 0;

        var unknownAdapter = new Mock<IN8nClient>();
        unknownAdapter.Setup(client => client.TriggerWebhookAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((_, __, ___) =>
            {
                attempts++;
                return Task.FromResult(new N8nWebhookDispatchResult(
                    N8nWebhookOutcome.UnknownOutcome, "n8n webhook call timed out"));
            });

        var consume = () => InvokeConsumerAsync(graph, execution, unknownAdapter.Object, NewDispatchMessage(execution));

        await consume.Should().NotThrowAsync(
            "an unknown outcome must NOT auto re-fire the provider call — that risks duplicate delivery");

        attempts.Should().Be(1, "the consumer makes one attempt and never redelivers an unknown outcome on its own");
        var stored = await LoadExecutionAsync(execution.Id);
        stored.Status.Should().Be(AutomationExecutionStatus.Failed,
            "an unknown outcome settles the execution as a terminal failure for reconciliation");
        stored.Error.Should().Contain("reconciliation required",
            "the persisted error signals the execution must be reconciled before any manual re-dispatch");
    }

    [Fact]
    public async Task FailingDispatch_DoesNotBlockIndependentDispatch()
    {
        var firstGraph = await SeedRuleAsync();
        var secondGraph = await SeedRuleAsync();
        var firstExecution = await SeedExecutionAsync(firstGraph);
        var secondExecution = await SeedExecutionAsync(secondGraph);

        var failingAdapter = new Mock<IN8nClient>();
        failingAdapter.Setup(client => client.TriggerWebhookAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new N8nWebhookDispatchResult(
                N8nWebhookOutcome.RetryableFailure, "n8n webhook call failed: n8n unreachable"));

        var consumeFailing = () => InvokeConsumerAsync(firstGraph, firstExecution, failingAdapter.Object, NewDispatchMessage(firstExecution));
        await consumeFailing.Should().ThrowAsync<N8nDispatchRetryableException>();

        var succeedingAdapter = new Mock<IN8nClient>();
        succeedingAdapter.Setup(client => client.TriggerWebhookAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new N8nWebhookDispatchResult(N8nWebhookOutcome.Succeeded, null));

        await InvokeConsumerAsync(secondGraph, secondExecution, succeedingAdapter.Object, NewDispatchMessage(secondExecution));

        (await LoadExecutionAsync(secondExecution.Id)).Status.Should().Be(AutomationExecutionStatus.Succeeded,
            "one failing dispatch must not head-of-line block unrelated dispatches");
    }

    // ------------------------------------------------------------------
    // Crash-boundary protocol (TAC-PF-FLOW-04 / M11). Under the
    // prepare/effect/settle protocol every crash point has a durable,
    // safe redelivery outcome:
    //   A  crash after Tx1 (claim Processing + execution Running) before the
    //      provider effect → redelivery must NOT re-fire; the interrupted
    //      attempt settles Failed with "reconciliation required".
    //   B  crash after a retryable Tx2 commit (evidence Queued + attempt++
    //      with the claim RELEASED) before ACK → redelivery re-acquires the
    //      claim and re-runs the attempt under the same execution identity.
    //   C  crash after a success Tx2 commit (claim Succeeded + execution
    //      Succeeded) before ACK → redelivery skips and never re-fires.
    // ------------------------------------------------------------------

    [Fact]
    public async Task CrashAfterPrepareCommit_RunningResidue_DoesNotRefireAndSettlesFailedWithReconciliation()
    {
        var graph = await SeedRuleAsync();
        var execution = await SeedRunningExecutionAsync(graph);

        var message = NewDispatchMessage(execution);
        await SeedProcessingClaimAsync(message, graph.WorkspaceId);

        var providerCalls = 0;
        var adapter = new Mock<IN8nClient>();
        adapter.Setup(client => client.TriggerWebhookAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((_, _, _) =>
            {
                providerCalls++;
                return Task.FromResult(new N8nWebhookDispatchResult(N8nWebhookOutcome.Succeeded, null));
            });

        await InvokeConsumerAsync(graph, execution, adapter.Object, message);

        providerCalls.Should().Be(0,
            "a redelivery of an interrupted attempt must never re-fire the provider call");

        var stored = await LoadExecutionAsync(execution.Id);
        stored.Status.Should().Be(AutomationExecutionStatus.Failed,
            "an interrupted attempt whose provider outcome is unknown settles as a terminal failure");
        stored.Error.Should().Contain("reconciliation required",
            "the persisted error signals the outcome must be reconciled before any manual re-dispatch");

        var claim = await LoadClaimAsync(message.EventId);
        claim.Should().NotBeNull();
        claim!.Status.Should().Be("Succeeded",
            "the claim must normalize to Succeeded so a later governed re-dispatch is not blocked");
    }

    [Fact]
    public async Task CrashAfterRetryableEvidenceCommit_ClaimReleased_RedeliveryReacquiresAndRetries()
    {
        var graph = await SeedRuleAsync();
        var execution = await SeedRequeuedExecutionAsync(graph);

        var message = NewDispatchMessage(execution);

        var providerCalls = 0;
        var adapter = new Mock<IN8nClient>();
        adapter.Setup(client => client.TriggerWebhookAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((_, _, _) =>
            {
                providerCalls++;
                return Task.FromResult(new N8nWebhookDispatchResult(N8nWebhookOutcome.Succeeded, null));
            });

        await InvokeConsumerAsync(graph, execution, adapter.Object, message);

        providerCalls.Should().Be(1,
            "redelivery after a released claim must re-run the attempt once");

        var stored = await LoadExecutionAsync(execution.Id);
        stored.Status.Should().Be(AutomationExecutionStatus.Succeeded);
        stored.AttemptCount.Should().Be(1,
            "the attempt evidence persisted before the crash must survive and not double-count");

        var claim = await LoadClaimAsync(message.EventId);
        claim.Should().NotBeNull("redelivery must re-acquire the released claim");
        claim!.Status.Should().Be("Succeeded");
    }

    [Fact]
    public async Task CrashAfterSucceededCommit_RedeliverySkipsAndNeverRefires()
    {
        var graph = await SeedRuleAsync();
        var execution = await SeedSucceededExecutionAsync(graph);

        var message = NewDispatchMessage(execution);
        await SeedSucceededClaimAsync(message, graph.WorkspaceId);

        var providerCalls = 0;
        var adapter = new Mock<IN8nClient>();
        adapter.Setup(client => client.TriggerWebhookAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((_, _, _) =>
            {
                providerCalls++;
                return Task.FromResult(new N8nWebhookDispatchResult(N8nWebhookOutcome.Succeeded, null));
            });

        await InvokeConsumerAsync(graph, execution, adapter.Object, message);

        providerCalls.Should().Be(0,
            "a succeeded execution must never be re-fired");

        (await LoadExecutionAsync(execution.Id)).Status.Should().Be(AutomationExecutionStatus.Succeeded);

        var claim = await LoadClaimAsync(message.EventId);
        claim.Should().NotBeNull();
        claim!.Status.Should().Be("Succeeded");
    }

    // --- helpers --------------------------------------------------------------

    private sealed record RuleGraph(Guid AccountId, Guid WorkspaceId, Guid RuleId);

    private async Task<RuleGraph> SeedRuleAsync()
    {
        var ownerId = Guid.NewGuid();
        var user = Domain.Identity.Users.User.Create($"n8n-{Guid.NewGuid():N}@example.com", "N8N User", "hashed", Now, true);
        var account = Domain.Accounts.Accounts.Account.Create("N8N Account", $"n8n-{Guid.NewGuid():N}", Domain.Accounts.Accounts.AccountType.Team, ownerId, Now);
        var accountId = account.Id;
        var workspace = Workspace.Create(accountId, ownerId, "Automation WS", $"n8n-{Guid.NewGuid():N}", Now);
        var member = WorkspaceMember.Create(accountId, workspace.Id, ownerId, WorkspaceRole.Owner, ownerId, Now);
        var board = Board.Create(accountId, workspace.Id, ownerId, "Board", null, Now);
        var group = BoardGroup.Create(accountId, workspace.Id, board.Id, "Todo", Color.Create("#808080"), FractionalIndex.Initial(), ownerId, Now);
        var item = BoardItem.CreateRoot(accountId, workspace.Id, board.Id, group.Id, "Task", FractionalIndex.Initial(), ownerId, Now);

        var config = AutomationConfiguration.Create(
            AutomationTriggerDefinition.Create("ItemAssigned"),
            AutomationActionDefinition.Create("Webhook", """{"webhookPath":"notrelix-card-assigned"}"""));
        var rule = AutomationRule.Create(accountId, workspace.Id, "Card assigned alert", config, ownerId, Now);
        rule.Enable(ownerId, Now);

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

    private async Task<AutomationExecution> SeedRunningExecutionAsync(RuleGraph graph)
    {
        var execution = AutomationExecution.Create(
            graph.AccountId, graph.WorkspaceId, graph.RuleId, Guid.NewGuid(), Now);
        execution.Start(Now);
        await using var seed = _db.CreateContext(SystemTenant());
        seed.AutomationExecutions.Add(execution);
        await seed.SaveChangesAsync();
        ((IHasDomainEvents)execution).ClearDomainEvents();
        return execution;
    }

    private async Task<AutomationExecution> SeedRequeuedExecutionAsync(RuleGraph graph)
    {
        var execution = AutomationExecution.Create(
            graph.AccountId, graph.WorkspaceId, graph.RuleId, Guid.NewGuid(), Now);
        execution.Start(Now);
        execution.RecordRetryableDispatchFailure("n8n unreachable", Now);
        await using var seed = _db.CreateContext(SystemTenant());
        seed.AutomationExecutions.Add(execution);
        await seed.SaveChangesAsync();
        ((IHasDomainEvents)execution).ClearDomainEvents();
        return execution;
    }

    private async Task<AutomationExecution> SeedSucceededExecutionAsync(RuleGraph graph)
    {
        var execution = AutomationExecution.Create(
            graph.AccountId, graph.WorkspaceId, graph.RuleId, Guid.NewGuid(), Now);
        execution.Start(Now);
        execution.Succeed(Now);
        await using var seed = _db.CreateContext(SystemTenant());
        seed.AutomationExecutions.Add(execution);
        await seed.SaveChangesAsync();
        ((IHasDomainEvents)execution).ClearDomainEvents();
        return execution;
    }

    private static MessagingProcessedEvent NewClaim(N8nDispatchRequestedV1 message, Guid workspaceId)
    {
        var claim = new MessagingProcessedEvent(
            eventId: message.EventId,
            consumerName: N8nDispatchProtocolEndpoints.DispatchEndpointName,
            sourceContext: null,
            messageName: message.MessageName,
            messageVersion: message.SchemaVersion,
            sourceEventId: message.SourceEventId,
            subjectType: null,
            subjectId: null,
            workspaceId: workspaceId,
            actorUserId: null,
            correlationId: message.CorrelationId.ToString(),
            causationId: null,
            claimedAt: Now);
        return claim;
    }

    private async Task SeedProcessingClaimAsync(N8nDispatchRequestedV1 message, Guid workspaceId)
    {
        await using var seed = _db.CreateContext(SystemTenant());
        seed.Set<MessagingProcessedEvent>().Add(NewClaim(message, workspaceId));
        await seed.SaveChangesAsync();
    }

    private async Task SeedSucceededClaimAsync(N8nDispatchRequestedV1 message, Guid workspaceId)
    {
        await using var seed = _db.CreateContext(SystemTenant());
        var claim = NewClaim(message, workspaceId);
        claim.MarkSucceeded(Now);
        seed.Set<MessagingProcessedEvent>().Add(claim);
        await seed.SaveChangesAsync();
    }

    private async Task<MessagingProcessedEvent?> LoadClaimAsync(Guid eventId)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        return await probe.Set<MessagingProcessedEvent>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.EventId == eventId
                && e.ConsumerName == N8nDispatchProtocolEndpoints.DispatchEndpointName);
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
        var tenant = SystemTenant();
        await using var context = _db.CreateContext(tenant);
        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(Now);

        var rls = new Notrelix.Infrastructure.Data.Rls.RlsSessionContext(
            context,
            Microsoft.Extensions.Options.Options.Create(
                new Notrelix.Application.Common.Data.Rls.RlsOptions { SetSessionContext = true }),
            tenant);

        var useCase = new N8nDispatchUseCase(
            context,
            new N8nWebhookActions(adapter),
            clockMock.Object);
        var claims = new MessageDeduplicationStore(
            context,
            clockMock.Object,
            new Notrelix.Infrastructure.Observability.Metrics.MetricsService());
        var consumer = new N8nDispatchConsumer(
            useCase,
            context,
            rls,
            claims,
            clockMock.Object,
            NullLogger<N8nDispatchConsumer>.Instance,
            new PipelineMetrics());

        var consumeContext = new Mock<ConsumeContext<N8nDispatchRequestedV1>>();
        consumeContext.SetupGet(c => c.Message).Returns(message);
        consumeContext.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(consumeContext.Object);
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
