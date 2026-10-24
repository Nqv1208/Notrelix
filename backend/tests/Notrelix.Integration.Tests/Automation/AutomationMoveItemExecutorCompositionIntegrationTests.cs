using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Events.Automation;
using Notrelix.Application.Features.Automation.Executions.Services;
using Notrelix.Application.Features.Automation.Ports.WorkManagement;
using Notrelix.Application.Features.Automation.Rules.Commands.SetAutomationRuleEnabled;
using Notrelix.Application.Features.Automation.Rules.Services;
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
/// Verifies the Automation MoveItem executor at the production composition
/// boundary. Work-owned business rejections are terminal Automation failures;
/// infrastructure failures remain retryable under the same durable execution
/// identity. The test uses the real Work action path and PostgreSQL so the
/// classification and mutation outcome are observed at their authoritative
/// boundaries.
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
    private async Task<CompositionGraph> SeedGraphAsync(string? actionConfigTemplate = null, bool wrongWorkspaceTarget = false, bool memberExecutor = false)
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
        var targetGroup = BoardGroup.Create(accountId, workspace.Id, board.Id, "Done", Color.Create("#00FF00"), FractionalIndex.Create("a1"), ownerId, Now);
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
        // AUT-020: the rule runs as its creator. The default executor is the workspace
        // Owner (allowed); memberExecutor binds the plain Member instead.
        var executorId = memberExecutor ? deniedUser.Id : executorUser.Id;
        var rule = AutomationRule.Create(accountId, workspace.Id, "Composition MoveItem", config, executorId, Now);
        rule.Enable(executorId, Now);

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
        CreateComposition(CompositionGraph graph, Func<ApplicationDbContext, IWorkActionPort, IWorkActionPort>? wrapPort = null)
    {
        var tenant = WorkspaceTenant(graph.AccountId, graph.WorkspaceId, graph.OwnerUserId);
        var context = _db.CreateContext(tenant);
        var clockMock = new Moq.Mock<Notrelix.Application.Common.Time.IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(Now);

        var moveUseCase = new Application.Features.WorkManagement.BoardItems.Services.MoveBoardItemUseCase(
            context, clockMock.Object,
            new Application.Features.WorkManagement.Ordering.WorkPlacementResolver(
                new Notrelix.Infrastructure.Data.WorkManagement.PostgresWorkOrderingScopeLock(context)));
        var idempotencyStore = new EfIdempotencyStore(
            context,
            System.TimeProvider.System,
            Microsoft.Extensions.Options.Options.Create(
                new Notrelix.Application.Common.Idempotency.IdempotencyOptions()));
        var authorizer = new Application.Features.WorkManagement.BoardItems.Services.WorkItemActionAuthorizer(
            new ResourceLocator(
                context, context, context, context, context, context,
                new Moq.Mock<Notrelix.Application.Common.Data.Rls.IRlsSessionContext>().Object),
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
        IWorkActionPort port = new WorkItemActionAdapter(actions);
        if (wrapPort is not null) port = wrapPort(context, port);
        var useCase = new AutomationMoveItemUseCase(context, port, clockMock.Object, new Notrelix.Infrastructure.Data.WorkManagement.PostgresOrderingConflictClassifier());
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
        var graph = await SeedGraphAsync(memberExecutor: true);
        var execution = await SeedExecutionAsync(graph);
        // Board children are decided on their Board (GOV-033): an ordinary
        // member of a Workspace-visible Board is now an Editor and may move
        // items. The denied principal is therefore made a board Observer
        // (Viewer) on a Private Board; the protected property is unchanged —
        // the canonical decision denies and nothing else may bypass it.

        await using (var restrict = _db.CreateContext(SystemTenant()))
        {
            var restricted = await restrict.Boards.SingleAsync(b => b.Id == graph.BoardId);
            restricted.ChangeVisibility(BoardVisibility.Private, restricted.CreatedBy!.Value, DateTimeOffset.UtcNow);
            restrict.BoardMembers.Add(BoardMember.Create(graph.BoardId, graph.DeniedUserId, BoardRole.Observer, DateTimeOffset.UtcNow));
            await restrict.SaveChangesAsync();
        }

        var outcome = await RunCompositionAsync(graph, graph.OwnerUserId, execution.Id);

        outcome.Status.Should().Be(AutomationExecutionStatus.Failed,
            "the canonical policy deny raised by the real Work chain is a terminal Automation business rejection");
        outcome.ItemGroupId.Should().Be(graph.SourceGroupId,
            "a denied executor must not mutate Work state");
        outcome.ItemVersion.Should().Be(1,
            "the denied request never reached the aggregate");

        var deniedRule = await ReadRuleAsync(graph);
        deniedRule.Status.Should().Be(AutomationRuleStatus.Disabled,
            "AUT-020: an executor that lost the permission stops the rule");
        deniedRule.DisabledReason.Should().Contain("no longer has permission");

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

    [Fact]
    public async Task MoveItemComposition_OrderingCollisionAtCommit_PropagatesTheStableConflict_AndRedeliveryReResolves()
    {
        // WM-V3-ORDER-005: a concurrent writer that bypasses the scope lock commits a
        // root item with the exact key the automation resolved. The unique ordering
        // index rejects the automation commit; the executor must surface
        // work.ordering.conflict without a second write inside the aborted
        // transaction, so the consumer transaction rolls back cleanly.
        var graph = await SeedGraphAsync();
        var execution = await SeedExecutionAsync(graph);
        var (_, context, consumer) = CreateComposition(graph, (ctx, inner) => new CollidingWorkActionPort(inner, ctx, _db, graph));
        var message = NewMoveItemMessage(execution.Id, graph.RuleId, graph.AccountId, graph.WorkspaceId, graph.OwnerUserId);
        var consumeContext = new Moq.Mock<ConsumeContext<AutomationMoveItemRequestedV1>>();
        consumeContext.SetupGet(c => c.Message).Returns(message);
        consumeContext.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await using (var tx = await context.Database.BeginTransactionAsync())
        {
            var act = () => consumer.Consume(consumeContext.Object);

            (await act.Should().ThrowAsync<ConflictException>(
                "a second SaveChanges in the aborted transaction would surface 25P02 instead"))
                .Which.ErrorCode.Should().Be("work.ordering.conflict");
            await tx.RollbackAsync();
        }

        var afterConflict = await ReadOutcomeAsync(graph, execution.Id);
        afterConflict.Status.Should().Be(AutomationExecutionStatus.Queued, "the rolled-back delivery records nothing");
        afterConflict.ItemGroupId.Should().Be(graph.SourceGroupId);
        afterConflict.ItemVersion.Should().Be(1);

        var redelivered = await RunCompositionAsync(graph, graph.OwnerUserId, execution.Id);
        redelivered.Status.Should().Be(AutomationExecutionStatus.Succeeded, "redelivery re-resolves placement after the colliding item");
        redelivered.ItemGroupId.Should().Be(graph.TargetGroupId);
    }

    /// <summary>
    /// After the real move resolved and tracked its new key, commits a root item
    /// with the same key in the target group from a separate connection that does
    /// not take the ordering lock.
    /// </summary>
    private sealed class CollidingWorkActionPort(
        IWorkActionPort inner, ApplicationDbContext context, PostgresTestContainer db, CompositionGraph graph) : IWorkActionPort
    {
        public async Task<WorkActionResult> MoveItemAsync(
            Guid itemId, Guid targetGroupId, Guid executionId, AutomationPrincipal automationPrincipal, CancellationToken cancellationToken)
        {
            var result = await inner.MoveItemAsync(itemId, targetGroupId, executionId, automationPrincipal, cancellationToken);
            var resolved = context.ChangeTracker.Entries<BoardItem>().Single(e => e.Entity.Id == itemId).Entity.Position;

            await using var other = db.CreateContext(SystemTenant());
            other.BoardItems.Add(BoardItem.CreateRoot(
                graph.AccountId, graph.WorkspaceId, graph.BoardId, targetGroupId, "Concurrent", resolved, graph.OwnerUserId, Now));
            await other.SaveChangesAsync(cancellationToken);
            return result;
        }

        public Task EnsureMoveItemAllowedAsync(
            Guid itemId, Guid targetGroupId, AutomationPrincipal automationPrincipal, CancellationToken cancellationToken) =>
            inner.EnsureMoveItemAllowedAsync(itemId, targetGroupId, automationPrincipal, cancellationToken);
    }

    // --- AUT-020: executor identity ------------------------------------------

    private async Task<AutomationRule> ReadRuleAsync(CompositionGraph graph)
    {
        await using var probe = _db.CreateContext(SystemTenant());
        return await probe.AutomationRules.IgnoreQueryFilters().SingleAsync(r => r.Id == graph.RuleId);
    }

    private async Task MakeBoardPrivateAsync(CompositionGraph graph, params (Guid UserId, BoardRole Role)[] members)
    {
        await using var restrict = _db.CreateContext(SystemTenant());
        var board = await restrict.Boards.SingleAsync(b => b.Id == graph.BoardId);
        board.ChangeVisibility(BoardVisibility.Private, board.CreatedBy!.Value, DateTimeOffset.UtcNow);
        foreach (var (userId, role) in members)
        {
            restrict.BoardMembers.Add(BoardMember.Create(graph.BoardId, userId, role, DateTimeOffset.UtcNow));
        }

        await restrict.SaveChangesAsync();
    }

    /// <summary>
    /// The authority over the real Work chain. The canonical access facts need the
    /// pipeline's open data-session transaction, so the returned scope owns one.
    /// </summary>
    private async Task<(AutomationExecutorAuthority Authority, IAsyncDisposable Scope)> CreateAuthorityAsync(CompositionGraph graph)
    {
        IWorkActionPort? captured = null;
        var (_, context, _) = CreateComposition(graph, (_, inner) => captured = inner);
        var tx = await context.Database.BeginTransactionAsync();
        return (new AutomationExecutorAuthority(captured!), tx);
    }

    private static AutomationActionDefinition MoveAction(CompositionGraph graph) =>
        AutomationActionDefinition.Create(
            "MoveItem", $$"""{"itemId":"{{graph.ItemId}}","targetGroupId":"{{graph.TargetGroupId}}"}""");

    [Fact]
    public async Task Authority_CreatorWithoutAccessToAPrivateBoard_CannotBindAMoveItemRule()
    {
        var graph = await SeedGraphAsync(memberExecutor: true);
        await MakeBoardPrivateAsync(graph); // the plain Member is not on the Private board

        var (authority, scope) = await CreateAuthorityAsync(graph);
        await using var _ = scope;
        var act = () => authority.EnsureCanExecuteAsync(
            graph.AccountId, graph.WorkspaceId, graph.DeniedUserId, MoveAction(graph), CancellationToken.None);

        var thrown = await act.Should().ThrowAsync<Exception>();
        thrown.Which.Should().Match<Exception>(e => e is ForbiddenException || e is NotFoundException,
            "a board the creator cannot see is rejected, never bound");
    }

    [Fact]
    public async Task Authority_CreatorWhoMayMoveTheItem_IsAccepted_AndUnknownTargetsAreRejected()
    {
        var graph = await SeedGraphAsync();
        var (authority, scope) = await CreateAuthorityAsync(graph);
        await using var _ = scope;

        await authority.EnsureCanExecuteAsync(
            graph.AccountId, graph.WorkspaceId, graph.OwnerUserId, MoveAction(graph), CancellationToken.None);

        var unknownGroup = AutomationActionDefinition.Create(
            "MoveItem", $$"""{"itemId":"{{graph.ItemId}}","targetGroupId":"{{Guid.NewGuid()}}"}""");
        await ((Func<Task>)(() => authority.EnsureCanExecuteAsync(
                graph.AccountId, graph.WorkspaceId, graph.OwnerUserId, unknownGroup, CancellationToken.None)))
            .Should().ThrowAsync<NotFoundException>("the target group must exist on the item's board");

        var foreign = AutomationActionDefinition.Create(
            "MoveItem", $$"""{"itemId":"{{graph.ForeignItemId}}","targetGroupId":"{{graph.ForeignGroupId}}"}""");
        await ((Func<Task>)(() => authority.EnsureCanExecuteAsync(
                graph.AccountId, graph.WorkspaceId, graph.OwnerUserId, foreign, CancellationToken.None)))
            .Should().ThrowAsync<Exception>("an item of another workspace is never reachable");
    }

    [Fact]
    public async Task EnableHandler_ExecutorWithoutAccessToThePrivateBoard_IsRejected_AndRuleStaysDisabled()
    {
        var graph = await SeedGraphAsync(memberExecutor: true);
        await using (var disable = _db.CreateContext(SystemTenant()))
        {
            var rule = await disable.AutomationRules.SingleAsync(r => r.Id == graph.RuleId);
            rule.Disable(graph.DeniedUserId, Now);
            await disable.SaveChangesAsync();
        }

        await MakeBoardPrivateAsync(graph);

        IWorkActionPort? port = null;
        var (_, context, _) = CreateComposition(graph, (_, inner) => port = inner);
        await using var tx = await context.Database.BeginTransactionAsync();
        var currentUser = new Moq.Mock<ICurrentUser>();
        currentUser.Setup(u => u.UserId).Returns(graph.OwnerUserId);
        var clock = new Moq.Mock<Notrelix.Application.Common.Time.IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(Now);
        var handler = new SetAutomationRuleEnabledCommandHandler(
            context, currentUser.Object, clock.Object, new AutomationExecutorAuthority(port!));

        var act = () => handler.Handle(new SetAutomationRuleEnabledCommand(graph.RuleId, true), CancellationToken.None);

        var thrown = await act.Should().ThrowAsync<Exception>();
        thrown.Which.Should().Match<Exception>(e => e is ForbiddenException || e is NotFoundException);
        (await ReadRuleAsync(graph)).Status.Should().Be(AutomationRuleStatus.Disabled,
            "enabling must not make a rule executable for an executor without access");
    }

    [Fact]
    public async Task MoveItemComposition_RunsAsTheExecutor_NotAsTheTriggerActor()
    {
        // The trigger actor is a plain Member who cannot move on the Private board;
        // the rule's executor (the workspace Owner) can. The move happens, attributed
        // to the executor.
        var graph = await SeedGraphAsync();
        await MakeBoardPrivateAsync(graph, (graph.OwnerUserId, BoardRole.Member));
        var execution = await SeedExecutionAsync(graph);

        var outcome = await RunCompositionAsync(graph, graph.DeniedUserId, execution.Id);

        outcome.Status.Should().Be(AutomationExecutionStatus.Succeeded);
        outcome.ItemGroupId.Should().Be(graph.TargetGroupId);
        await using var probe = _db.CreateContext(SystemTenant());
        var item = await probe.BoardItems.IgnoreQueryFilters().SingleAsync(i => i.Id == graph.ItemId);
        item.UpdatedBy.Should().Be(graph.OwnerUserId, "the audit trail records the executor, not the trigger actor");
        (await ReadRuleAsync(graph)).Status.Should().Be(AutomationRuleStatus.Active);
    }

    [Fact]
    public async Task MoveItemComposition_ExecutorLosesBoardAccess_DisablesTheRule_AndDoesNotMove()
    {
        var graph = await SeedGraphAsync(memberExecutor: true);
        await MakeBoardPrivateAsync(graph); // the executor is no longer on the board
        var execution = await SeedExecutionAsync(graph);

        var outcome = await RunCompositionAsync(graph, graph.OwnerUserId, execution.Id);

        outcome.Status.Should().Be(AutomationExecutionStatus.Failed);
        outcome.ItemGroupId.Should().Be(graph.SourceGroupId);
        outcome.ItemVersion.Should().Be(1);
        var rule = await ReadRuleAsync(graph);
        rule.Status.Should().Be(AutomationRuleStatus.Disabled);
        rule.DisabledReason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task MoveItemComposition_LegacyRuleWithoutExecutor_FailsClosed_AndIsDisabled()
    {
        var graph = await SeedGraphAsync();
        await using (var legacy = _db.CreateContext(SystemTenant()))
        {
            await legacy.Database.ExecuteSqlRawAsync(
                "UPDATE automation.automation_rules SET executor_user_id = NULL WHERE id = {0}", graph.RuleId);
        }

        var execution = await SeedExecutionAsync(graph);

        var outcome = await RunCompositionAsync(graph, graph.OwnerUserId, execution.Id);

        outcome.Status.Should().Be(AutomationExecutionStatus.Failed);
        outcome.ItemGroupId.Should().Be(graph.SourceGroupId, "a rule with no executor never runs as someone else");
        var rule = await ReadRuleAsync(graph);
        rule.Status.Should().Be(AutomationRuleStatus.Disabled);
        rule.ExecutorUserId.Should().BeNull();
        rule.DisabledReason.Should().Contain("no executor");
    }

    private async Task RunLifecycleConsumerAsync<TMessage>(
        CompositionGraph graph, TMessage message, Func<AutomationExecutorLifecycle, IConsumer<TMessage>> create)
        where TMessage : class
    {
        await using var context = _db.CreateContext(WorkspaceTenant(graph.AccountId, graph.WorkspaceId, graph.OwnerUserId));
        var clock = new Moq.Mock<Notrelix.Application.Common.Time.IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(Now);
        var lifecycle = new AutomationExecutorLifecycle(context, clock.Object, NullLogger<AutomationExecutorLifecycle>.Instance);
        var consume = new Moq.Mock<ConsumeContext<TMessage>>();
        consume.SetupGet(c => c.Message).Returns(message);
        consume.SetupGet(c => c.CancellationToken).Returns(CancellationToken.None);

        await create(lifecycle).Consume(consume.Object);
        await context.SaveChangesAsync();
    }

    private async Task AssertNoFurtherExecutionsAsync(CompositionGraph graph)
    {
        await using var context = _db.CreateContext(WorkspaceTenant(graph.AccountId, graph.WorkspaceId, graph.OwnerUserId));
        var clock = new Moq.Mock<Notrelix.Application.Common.Time.IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(Now);
        var evaluator = new Application.Features.Automation.Events.N8nAutomationRuleEvaluator(
            context, new IntegrationEventCollector(), clock.Object);
        await evaluator.ExecuteAsync(
            new Application.Features.Automation.Events.AutomationTriggerContext(
                graph.AccountId, graph.WorkspaceId, "ItemCreated", Guid.NewGuid(), graph.DeniedUserId,
                Guid.NewGuid(), null, Now.AddMinutes(1)),
            CancellationToken.None);
        await context.SaveChangesAsync();

        (await context.AutomationExecutions.CountAsync(e => e.RuleId == graph.RuleId))
            .Should().Be(0, "a disabled rule starts no further executions");
    }

    [Fact]
    public async Task ExecutorRemoved_DisablesTheRuleWithReason_AndNoFurtherExecutionsStart()
    {
        var graph = await SeedGraphAsync();
        var message = new Application.Events.Workspaces.WorkspaceMemberRemovedIntegrationEvent(
            Guid.NewGuid(), graph.AccountId, graph.WorkspaceId, graph.OwnerUserId, Guid.NewGuid(), null, null, Now);

        await RunLifecycleConsumerAsync(graph, message, l => new WorkspaceMemberRemovedAutomationConsumer(l));

        var rule = await ReadRuleAsync(graph);
        rule.Status.Should().Be(AutomationRuleStatus.Disabled);
        rule.DisabledReason.Should().Be(AutomationExecutorLifecycle.RemovedReason);
        await AssertNoFurtherExecutionsAsync(graph);
    }

    [Fact]
    public async Task ExecutorSuspended_DisablesTheRuleWithReason()
    {
        var graph = await SeedGraphAsync();
        var message = new Application.Events.Workspaces.WorkspaceMemberSuspendedIntegrationEvent(
            Guid.NewGuid(), graph.AccountId, graph.WorkspaceId, graph.OwnerUserId, Guid.NewGuid(), null, null, Now);

        await RunLifecycleConsumerAsync(graph, message, l => new WorkspaceMemberSuspendedAutomationConsumer(l));

        var rule = await ReadRuleAsync(graph);
        rule.Status.Should().Be(AutomationRuleStatus.Disabled);
        rule.DisabledReason.Should().Be(AutomationExecutorLifecycle.SuspendedReason);
    }

    [Fact]
    public async Task ExecutorDemoted_DisablesTheRule_ButAPromotionDoesNot()
    {
        var graph = await SeedGraphAsync();
        Application.Events.Workspaces.WorkspaceMemberRoleChangedIntegrationEvent Change(bool demotion) =>
            new(Guid.NewGuid(), graph.AccountId, graph.WorkspaceId, graph.OwnerUserId,
                demotion ? "Owner" : "Member", demotion ? "Member" : "Owner", demotion, Guid.NewGuid(), null, null, Now);

        await RunLifecycleConsumerAsync(graph, Change(demotion: false), l => new WorkspaceMemberRoleChangedAutomationConsumer(l));
        (await ReadRuleAsync(graph)).Status.Should().Be(AutomationRuleStatus.Active);

        await RunLifecycleConsumerAsync(graph, Change(demotion: true), l => new WorkspaceMemberRoleChangedAutomationConsumer(l));
        var rule = await ReadRuleAsync(graph);
        rule.Status.Should().Be(AutomationRuleStatus.Disabled);
        rule.DisabledReason.Should().Be(AutomationExecutorLifecycle.DemotedReason);
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
