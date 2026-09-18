using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Events.Automation;
using Notrelix.Application.Features.Automation.Executions.Services;
using Notrelix.Application.Features.WorkManagement.Public.ItemMovement;
using Notrelix.Domain.Automation.Executions;
using Notrelix.Domain.Automation.Rules;
using Notrelix.Domain.Automation.RulesEngine;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.CrossContext.Automation.WorkManagement;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Messaging.Consumers.Automation;
using Notrelix.Infrastructure.Operations.Idempotency;
using Notrelix.Infrastructure.Services;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Automation;

/// <summary>
/// TAC-AI-011A composition (M12A) — the production Automation MoveItem entry
/// point running over the real Automation→Work chain: executor → real
/// WorkItemActionAdapter → real WorkItemActions → MoveBoardItemUseCase, all on
/// one ApplicationDbContext against real PostgreSQL. The entry is the executor
/// (never IWorkActionPort alone, per backend/tests.md #71). Target business
/// rejections surfaced by the real Work chain (not found, unauthorized, wrong
/// workspace, business rule) are terminal Automation failures, while technical
/// failures stay retryable under the stable ExecutionId — the classification
/// runs in the executor, which is the only placement that can see both sides.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class AutomationMoveItemExecutorCompositionIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public AutomationMoveItemExecutorCompositionIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed record CompositionGraph(
        Guid AccountId,
        Guid WorkspaceId,
        Guid RuleId,
        Guid ItemId,
        Guid BoardId,
        Guid SourceGroupId,
        Guid TargetGroupId,
        Guid OwnerUserId,
        Guid DeniedUserId,
        Guid ForeignItemId,
        Guid ForeignGroupId);

    /// <summary>
    /// Seeds the full Automation→Work graph: account-scoped workspace with an
    /// Owner (allowed executor) and a plain Member (canonically denied), a
    /// separate foreign workspace item the primary automation must never reach,
    /// and an enabled MoveItem rule whose configuration the executor parses.
    /// </summary>
    private async Task<CompositionGraph> SeedGraphAsync(string? actionConfigTemplate = null, bool wrongWorkspaceTarget = false)
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var executorUser = User.Create($"cmp-{Guid.NewGuid():N}@example.com", "Composition Owner", "hashed", Now, true);
        var deniedUser = User.Create($"cmp-{Guid.NewGuid():N}@example.com", "Composition Member", "hashed", Now, true);
        var workspace = Workspace.Create(accountId, ownerId, "Composition WS", $"cmp-{Guid.NewGuid():N}", Now);
        var member = WorkspaceMember.Create(accountId, workspace.Id, executorUser.Id, WorkspaceRole.Owner, ownerId, Now);
        var deniedMember = WorkspaceMember.Create(accountId, workspace.Id, deniedUser.Id, WorkspaceRole.Member, ownerId, Now);
        var board = Board.Create(accountId, workspace.Id, ownerId, "Board", null, Now);
        var sourceGroup = BoardGroup.Create(accountId, workspace.Id, board.Id, "Todo", Color.Create("#808080"), FractionalIndex.Initial(), ownerId, Now);
        var targetGroup = BoardGroup.Create(accountId, workspace.Id, board.Id, "Done", Color.Create("#00FF00"), FractionalIndex.Initial(), ownerId, Now);
        var item = BoardItem.CreateRoot(accountId, workspace.Id, board.Id, sourceGroup.Id, "Task", FractionalIndex.Initial(), ownerId, Now);

        var foreignWs = Workspace.Create(accountId, ownerId, "Composition Foreign WS", $"cmp-f-{Guid.NewGuid():N}", Now);
        var foreignBoard = Board.Create(accountId, foreignWs.Id, ownerId, "Foreign Board", null, Now);
        var foreignGroup = BoardGroup.Create(accountId, foreignWs.Id, foreignBoard.Id, "Foreign", Color.Create("#000080"), FractionalIndex.Initial(), ownerId, Now);
        var foreignItem = BoardItem.CreateRoot(accountId, foreignWs.Id, foreignBoard.Id, foreignGroup.Id, "Foreign Task", FractionalIndex.Initial(), ownerId, Now);

        var actionConfig = (actionConfigTemplate ?? (wrongWorkspaceTarget
                ? $$"""{"itemId":"{{foreignItem.Id}}","targetGroupId":"{{foreignGroup.Id}}"}"""
                : $$"""{"itemId":"{{item.Id}}","targetGroupId":"{{targetGroup.Id}}"}"""))
            .Replace("{itemId}", item.Id.ToString());
        var config = AutomationConfiguration.Create(
            AutomationTriggerDefinition.Create("ItemCreated", null),
            AutomationActionDefinition.Create("MoveItem", actionConfig));
        var rule = AutomationRule.Create(accountId, workspace.Id, "Composition MoveItem", config, ownerId, Now);
        rule.Enable(ownerId, Now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Users.Add(executorUser);
        seed.Users.Add(deniedUser);
        seed.Workspaces.Add(workspace);
        seed.Workspaces.Add(foreignWs);
        seed.WorkspaceMembers.Add(member);
        seed.WorkspaceMembers.Add(deniedMember);
        seed.Boards.Add(board);
        seed.Boards.Add(foreignBoard);
        seed.BoardGroups.Add(sourceGroup);
        seed.BoardGroups.Add(targetGroup);
        seed.BoardGroups.Add(foreignGroup);
        seed.BoardItems.Add(item);
        seed.BoardItems.Add(foreignItem);
        seed.AutomationRules.Add(rule);
        await seed.SaveChangesAsync();

        await using var grant = _db.CreateContext(SystemTenant());
        var projection = new AccessGrantProjectionService(grant);
        await projection.SyncWorkspaceMemberGrantAsync(
            accountId, workspace.Id, executorUser.Id, WorkspaceRole.Owner, Now, CancellationToken.None);
        await projection.SyncWorkspaceMemberGrantAsync(
            accountId, workspace.Id, deniedUser.Id, WorkspaceRole.Member, Now, CancellationToken.None);
        await grant.SaveChangesAsync();

        return new CompositionGraph(
            accountId, workspace.Id, rule.Id, item.Id, board.Id, sourceGroup.Id, targetGroup.Id,
            executorUser.Id, deniedUser.Id, foreignItem.Id, foreignGroup.Id);
    }

    /// <summary>
    /// Builds the production Automation MoveItem composition on ONE workspace
    /// scoped ApplicationDbContext: the executor, its Automation DbContext, and
    /// the real Work chain (adapter → WorkItemActions → MoveBoardItemUseCase →
    /// canonical authorizer/idempotency/facts) all share the same context just
    /// as DI composes them.
    /// </summary>
    private (AutomationMoveItemUseCase UseCase, ApplicationDbContext Context, AutomationMoveItemDispatchConsumer Consumer)
        CreateComposition(CompositionGraph graph)
    {
        var tenant = WorkspaceTenant(graph.AccountId, graph.WorkspaceId, graph.OwnerUserId);
        var context = _db.CreateContext(tenant);
        var clockMock = new Moq.Mock<Notrelix.Application.Common.Time.IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(Now);

        var moveUseCase = new Application.Features.WorkManagement.BoardItems.Services.MoveBoardItemUseCase(
            context, clockMock.Object);
        var idempotencyStore = new EfIdempotencyStore(
            context,
            System.TimeProvider.System,
            Microsoft.Extensions.Options.Options.Create(
                new Notrelix.Application.Common.Idempotency.IdempotencyOptions()));
        var authorizer = new Application.Features.WorkManagement.BoardItems.Services.WorkItemActionAuthorizer(
            new ResourceLocator(
                context, context, context, context, context, context),
            Notrelix.Application.Common.Requests.Execution.RequestDescriptorRegistry
                .Create(typeof(IWorkItemActions).Assembly),
            new PostgresAccessFactsProvider(
                context,
                System.TimeProvider.System,
                new PostgresPageAuthorizationFacts(context),
                new FakeBillingSubscriptionFacts()),
            new Notrelix.Application.Features.Governance.Authorization.AccessPolicyEngine());
        var actions = new Application.Features.WorkManagement.BoardItems.Services.WorkItemActions(
            moveUseCase,
            context,
            authorizer,
            idempotencyStore);
        var port = new WorkItemActionAdapter(actions);
        var useCase = new AutomationMoveItemUseCase(context, port, clockMock.Object);
        var consumer = new AutomationMoveItemDispatchConsumer(
            useCase,
            NullLogger<AutomationMoveItemDispatchConsumer>.Instance,
            new PipelineMetrics());

        return (useCase, context, consumer);
    }

    private sealed record CompositionOutcome(
        AutomationExecutionStatus Status,
        int AttemptCount,
        Guid ItemGroupId,
        long ItemVersion);

    private async Task<CompositionOutcome> RunCompositionAsync(
        CompositionGraph graph,
        Guid actorUserId,
        Guid executionId,
        bool withAmbientTransaction = true)
    {
        var (useCase, context, consumer) = CreateComposition(graph);
        var message = NewMoveItemMessage(executionId, graph.RuleId, graph.AccountId, graph.WorkspaceId, actorUserId);

        var consumeContext = new Moq.Mock<ConsumeContext<AutomationMoveItemRequestedV1>>();
        consumeContext.SetupGet(c => c.Message).Returns(message);
        consumeContext.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        if (withAmbientTransaction)
        {
            await using var tx = await context.Database.BeginTransactionAsync();
            await consumer.Consume(consumeContext.Object);
            await context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        else
        {
            await useCase.ExecuteAsync(message, CancellationToken.None);
        }

        return await ReadOutcomeAsync(graph, executionId);
    }

    /// <summary>
    /// Seeds the durable process-state reference the executor loads by id. The
    /// execution must exist before dispatch: the executor treats a missing
    /// execution as a terminal no-op, not a work item invocation.
    /// </summary>
    private async Task<AutomationExecution> SeedExecutionAsync(CompositionGraph graph)
    {
        var execution = AutomationExecution.Create(
            graph.AccountId, graph.WorkspaceId, graph.RuleId, Guid.NewGuid(), Now);
        await using var seed = _db.CreateContext(SystemTenant());
        seed.AutomationExecutions.Add(execution);
        await seed.SaveChangesAsync();
        ((Notrelix.Domain.Common.IHasDomainEvents)execution).ClearDomainEvents();
        return execution;
    }

    private async Task<CompositionOutcome> ReadOutcomeAsync(CompositionGraph graph, Guid executionId)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        var execution = await probe.AutomationExecutions
            .IgnoreQueryFilters()
            .SingleAsync(e => e.Id == executionId);
        var item = await probe.BoardItems.IgnoreQueryFilters().SingleAsync(i => i.Id == graph.ItemId);
        return new CompositionOutcome(execution.Status, execution.AttemptCount, item.GroupId, item.Version);
    }

    [Fact]
    public async Task MoveItemComposition_OwnerExecutorThroughRealChain_SucceedsAndMovesItem()
    {
        var graph = await SeedGraphAsync();
        var execution = await SeedExecutionAsync(graph);

        var outcome = await RunCompositionAsync(graph, graph.OwnerUserId, execution.Id);

        outcome.Status.Should().Be(AutomationExecutionStatus.Succeeded,
            "an allowed executor completes the terminal success");
        outcome.ItemGroupId.Should().Be(graph.TargetGroupId,
            "the real Work chain mutates the item under the target's authority");
        outcome.ItemVersion.Should().Be(2,
            "exactly one move mutation: creation seeds version 1, one move increments to 2");
        outcome.AttemptCount.Should().Be(0);
    }

    [Fact]
    public async Task MoveItemComposition_DeniedMemberThroughRealChain_IsTerminalFailureNoMutationNoDedup()
    {
        var graph = await SeedGraphAsync();
        var execution = await SeedExecutionAsync(graph);

        var outcome = await RunCompositionAsync(graph, graph.DeniedUserId, execution.Id);

        outcome.Status.Should().Be(AutomationExecutionStatus.Failed,
            "the canonical policy deny raised by the real Work chain is a terminal Automation business rejection");
        outcome.ItemGroupId.Should().Be(graph.SourceGroupId,
            "a denied executor must not mutate Work state");
        outcome.ItemVersion.Should().Be(1,
            "the denied request never reached the aggregate");

        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.IdempotencyRecords
            .AnyAsync(r => r.KeyHash == Sha256($"move-item:{execution.Id:N}")))
            .Should().BeFalse("a deny must never begin a dedup record");
        (await verify.Set<Notrelix.Infrastructure.Data.Messaging.MessagingOutboxMessage>()
            .IgnoreQueryFilters()
            .AnyAsync(m => m.MessageName == "board_item.moved" && m.WorkspaceId == graph.WorkspaceId))
            .Should().BeFalse("no outward event for a denied executor");
    }

    [Fact]
    public async Task MoveItemComposition_WrongWorkspaceItemThroughRealChain_IsTerminalFailureNoMutation()
    {
        var scopedGraph = await SeedGraphAsync(wrongWorkspaceTarget: true);
        var execution = await SeedExecutionAsync(scopedGraph);

        var outcome = await RunCompositionAsync(scopedGraph, scopedGraph.OwnerUserId, execution.Id);

        outcome.Status.Should().Be(AutomationExecutionStatus.Failed,
            "a wrong-workspace target is a deterministic target-owned rejection");
        outcome.ItemGroupId.Should().Be(scopedGraph.SourceGroupId,
            "the primary item must not be touched");

        await using var verify = _db.CreateContext(SystemTenant());
        var foreignItem = await verify.BoardItems.IgnoreQueryFilters()
            .SingleAsync(i => i.Id == scopedGraph.ForeignItemId);
        foreignItem.GroupId.Should().Be(scopedGraph.ForeignGroupId,
            "the foreign item must not be mutated either");
    }

    [Fact]
    public async Task MoveItemComposition_UnknownTargetGroupThroughRealChain_IsTerminalFailureNoMutation()
    {
        var unknownTarget = Guid.NewGuid();
        var graph = await SeedGraphAsync($$"""{"itemId":"{itemId}","targetGroupId":"{{unknownTarget}}"}""");
        var execution = await SeedExecutionAsync(graph);

        var outcome = await RunCompositionAsync(graph, graph.OwnerUserId, execution.Id);

        outcome.Status.Should().Be(AutomationExecutionStatus.Failed,
            "a missing target group rejects the move as a business failure, which the Automation records as terminal");
        outcome.ItemGroupId.Should().Be(graph.SourceGroupId,
            "the rejected mutation must not change Work state");
        outcome.ItemVersion.Should().Be(1);
    }

    [Fact]
    public async Task MoveItemComposition_TechnicalFailureThroughRealChain_StaysRetryableUnderSameExecutionId()
    {
        // No ambient transaction: the real idempotency store fails technically
        // (BeginAsync requires the pipeline-owned transaction). A technical
        // failure must NOT fold into a business rejection — the executor keeps
        // the execution retryable under the stable ExecutionId.
        var graph = await SeedGraphAsync();
        var execution = await SeedExecutionAsync(graph);
        var (useCase, context, _) = CreateComposition(graph);
        var message = NewMoveItemMessage(execution.Id, graph.RuleId, graph.AccountId, graph.WorkspaceId, graph.OwnerUserId);

        var result = await useCase.ExecuteAsync(message, CancellationToken.None);

        result.Should().BeFalse("a technical target failure is retryable, not terminal");
        var outcome = await ReadOutcomeAsync(graph, execution.Id);
        outcome.Status.Should().Be(AutomationExecutionStatus.Queued,
            "a retryable failure records evidence and re-queues the execution");
        outcome.AttemptCount.Should().Be(1);
        outcome.ItemGroupId.Should().Be(graph.SourceGroupId,
            "no Work mutation may be committed for a retryable technical failure");

        // Redelivery under the SAME execution id (with the pipeline transaction)
        // now completes the move.
        var retryOutcome = await RunCompositionAsync(graph, graph.OwnerUserId, execution.Id);
        retryOutcome.Status.Should().Be(AutomationExecutionStatus.Succeeded,
            "the retry reuses the identical execution identity and terminal success");
        retryOutcome.ItemGroupId.Should().Be(graph.TargetGroupId);
        retryOutcome.ItemVersion.Should().Be(2,
            "the retried delivery performs exactly one logical move");
    }

    // --- helpers --------------------------------------------------------------

    private static AutomationMoveItemRequestedV1 NewMoveItemMessage(
        Guid executionId, Guid ruleId, Guid accountId, Guid workspaceId, Guid actorUserId) =>
        new(Guid.CreateVersion7(), executionId, ruleId, accountId, workspaceId, actorUserId,
            DateTimeOffset.UtcNow, Guid.NewGuid(), null);

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    private static ICurrentTenantContext WorkspaceTenant(Guid accountId, Guid workspaceId, Guid userId)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(accountId, workspaceId, userId);
        return tenant;
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
}