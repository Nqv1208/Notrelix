using Notrelix.Application.Features.Automation.Ports.WorkManagement;
using Notrelix.Application.Features.WorkManagement.Public.ItemMovement;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.CrossContext.Automation.WorkManagement;
using Notrelix.Infrastructure.Data;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Automation;

/// <summary>
/// TAC-XPK-001/002/003 — the flagship Automation→Work target-mutation chain:
/// the Automation work-action port routes through the pure ACL and the
/// WorkManagement Public move action (one producer use case), mutates Work
/// state under the target's authority, and target business rejection is a
/// producer business failure — not a transport retry and not an Automation
/// mutation of Work persistence.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class AutomationWorkActionChainIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public AutomationWorkActionChainIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static ICurrentTenantContext SystemTenant()
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

    private sealed record ChainGraph(
        Guid AccountId,
        Guid WorkspaceId,
        Guid ItemId,
        Guid BoardId,
        Guid SourceGroupId,
        Guid TargetGroupId,
        Guid ExecutorUserId,
        Guid MemberUserId);

    private async Task<ChainGraph> SeedChainAsync()
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var executorUser = User.Create($"xpk-{Guid.NewGuid():N}@example.com", "XPK Executor", "hashed", Now, true);
        var memberUser = User.Create($"xpk-{Guid.NewGuid():N}@example.com", "XPK Member", "hashed", Now, true);
        var workspace = Workspace.Create(accountId, ownerId, "XPK Workspace", $"xpk-{Guid.NewGuid():N}", Now);
        var member = WorkspaceMember.Create(accountId, workspace.Id, executorUser.Id, WorkspaceRole.Owner, ownerId, Now);
        var restrictedMember = WorkspaceMember.Create(accountId, workspace.Id, memberUser.Id, WorkspaceRole.Member, ownerId, Now);
        var board = Board.Create(accountId, workspace.Id, ownerId, "Board", null, Now);
        var sourceGroup = BoardGroup.Create(accountId, workspace.Id, board.Id, "Todo", Domain.SharedKernel.Color.Create("#808080"), FractionalIndex.Initial(), ownerId, Now);
        var targetGroup = BoardGroup.Create(accountId, workspace.Id, board.Id, "Done", Domain.SharedKernel.Color.Create("#00FF00"), FractionalIndex.Initial(), ownerId, Now);
        var item = BoardItem.CreateRoot(accountId, workspace.Id, board.Id, sourceGroup.Id, "Task", FractionalIndex.Initial(), ownerId, Now);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Users.Add(executorUser);
        seed.Users.Add(memberUser);
        seed.Workspaces.Add(workspace);
        seed.WorkspaceMembers.Add(member);
        seed.WorkspaceMembers.Add(restrictedMember);
        seed.Boards.Add(board);
        seed.BoardGroups.Add(sourceGroup);
        seed.BoardGroups.Add(targetGroup);
        seed.BoardItems.Add(item);
        await seed.SaveChangesAsync();

        await using var grant = _db.CreateContext(SystemTenant());
        var projection = new Notrelix.Infrastructure.Data.Authz.AccessGrantProjectionService(grant);
        await projection.SyncWorkspaceMemberGrantAsync(
            accountId, workspace.Id, executorUser.Id, WorkspaceRole.Owner, Now, CancellationToken.None);
        await projection.SyncWorkspaceMemberGrantAsync(
            accountId, workspace.Id, memberUser.Id, WorkspaceRole.Member, Now, CancellationToken.None);
        await grant.SaveChangesAsync();

        return new ChainGraph(accountId, workspace.Id, item.Id, board.Id, sourceGroup.Id, targetGroup.Id, executorUser.Id, memberUser.Id);
    }

    private (IWorkActionPort Port, ApplicationDbContext WorkContext) CreatePort(ChainGraph graph)
    {
        // The runtime composition resolves the adapter against the tenant
        // context of the automation execution's workspace. The delivery
        // pipeline commits the scoped context after the consumer returns.
        var tenant = WorkspaceTenant(graph.AccountId, graph.WorkspaceId, graph.ExecutorUserId);
        var workContext = _db.CreateContext(tenant);
        var clockMock = new Moq.Mock<Notrelix.Application.Common.Time.IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(Now);
        var workItemActions = new Application.Features.WorkManagement.BoardItems.Services.MoveBoardItemUseCase(
            workContext, clockMock.Object);
        var idempotencyStore = new Notrelix.Infrastructure.Operations.Idempotency.EfIdempotencyStore(
            workContext,
            System.TimeProvider.System,
            Microsoft.Extensions.Options.Options.Create(
                new Notrelix.Application.Common.Idempotency.IdempotencyOptions()));

        // Canonical authorization: same locator, same facts provider, same
        // policy engine as the HTTP pipeline, evaluated over an open
        // connection and transaction exactly like the canonical behaviors.
        var authorizer = new Application.Features.WorkManagement.BoardItems.Services.WorkItemActionAuthorizer(
            new Notrelix.Infrastructure.Services.ResourceLocator(
                workContext, workContext, workContext, workContext, workContext),
            Notrelix.Application.Common.Requests.Execution.RequestDescriptorRegistry
                .Create(typeof(Application.Features.WorkManagement.Public.ItemMovement.IWorkItemActions).Assembly),
            new Notrelix.Infrastructure.Data.Authz.PostgresAccessFactsProvider(
                workContext,
                System.TimeProvider.System,
                new Notrelix.Infrastructure.Data.Authz.PostgresPageAuthorizationFacts(workContext)),
            new Notrelix.Application.Common.Security.AccessPolicyEngine());

        var actions = new Application.Features.WorkManagement.BoardItems.Services.WorkItemActions(
            workItemActions,
            workContext,
            authorizer,
            idempotencyStore);
        return (new WorkItemActionAdapter(actions), workContext);
    }

    [Fact]
    public async Task AutomationMoveItem_MutatesWorkThroughTargetAuthority()
    {
        var graph = await SeedChainAsync();
        var (port, workContext) = CreatePort(graph);
        var executionId = Guid.CreateVersion7();

        await using var tx = await workContext.Database.BeginTransactionAsync();
        var result = await port.MoveItemAsync(
            graph.ItemId, graph.TargetGroupId, executionId,
            new AutomationPrincipal(graph.AccountId, graph.ExecutorUserId, graph.WorkspaceId),
            CancellationToken.None);
        await workContext.SaveChangesAsync();
        await tx.CommitAsync();

        result.ItemId.Should().Be(graph.ItemId);
        result.GroupId.Should().Be(graph.TargetGroupId);

        // Verify through a fresh producer-owned read.
        await using var verify = _db.CreateContext(SystemTenant());
        var item = await verify.BoardItems.SingleAsync(i => i.Id == graph.ItemId);
        item.GroupId.Should().Be(graph.TargetGroupId);
    }

    [Fact]
    public async Task AutomationMoveItem_SameOperationIdAndPayload_ReplaysWithoutSecondMutation()
    {
        var graph = await SeedChainAsync();
        var (port, workContext) = CreatePort(graph);
        var executionId = Guid.CreateVersion7();

        await using var tx = await workContext.Database.BeginTransactionAsync();
        var first = await port.MoveItemAsync(
            graph.ItemId, graph.TargetGroupId, executionId,
            new AutomationPrincipal(graph.AccountId, graph.ExecutorUserId, graph.WorkspaceId),
            CancellationToken.None);
        await workContext.SaveChangesAsync();

        var second = await port.MoveItemAsync(
            graph.ItemId, graph.TargetGroupId, executionId,
            new AutomationPrincipal(graph.AccountId, graph.ExecutorUserId, graph.WorkspaceId),
            CancellationToken.None);

        second.Should().Be(first, "the duplicate retries one logical move through the committed dedup record");
        await tx.CommitAsync();

        await using var verify = _db.CreateContext(SystemTenant());
        var item = await verify.BoardItems.SingleAsync(i => i.Id == graph.ItemId);
        item.GroupId.Should().Be(graph.TargetGroupId);
        item.Version.Should().Be(2, "exactly one move mutation: creation seeds version 1, one move increments to 2");
    }

    [Fact]
    public async Task AutomationMoveItem_SameOperationIdDifferentPayload_IsDeterministicConflict()
    {
        var graph = await SeedChainAsync();
        var (port, workContext) = CreatePort(graph);
        var executionId = Guid.CreateVersion7();

        await using var tx = await workContext.Database.BeginTransactionAsync();
        await port.MoveItemAsync(
            graph.ItemId, graph.TargetGroupId, executionId,
            new AutomationPrincipal(graph.AccountId, graph.ExecutorUserId, graph.WorkspaceId),
            CancellationToken.None);
        await workContext.SaveChangesAsync();

        var conflicting = () => port.MoveItemAsync(
            graph.ItemId, graph.SourceGroupId, executionId,
            new AutomationPrincipal(graph.AccountId, graph.ExecutorUserId, graph.WorkspaceId),
            CancellationToken.None);

        await conflicting.Should().ThrowAsync<WorkItemOperationConflictException>();
        await tx.RollbackAsync();
    }

    [Fact]
    public async Task AutomationMoveItem_WithInvalidTargetGroup_IsBusinessFailureNotRetry()
    {
        var graph = await SeedChainAsync();
        var (port, workContext) = CreatePort(graph);
        var unrelatedGroup = Guid.CreateVersion7();

        await using var tx = await workContext.Database.BeginTransactionAsync();
        var act = () => port.MoveItemAsync(
            graph.ItemId, unrelatedGroup, Guid.CreateVersion7(),
            new AutomationPrincipal(graph.AccountId, graph.ExecutorUserId, graph.WorkspaceId),
            CancellationToken.None);

        // The Work producer rejects the mutation as a business failure; the
        // Automation process records it as terminal business rejection — the
        // delivery mechanism must not treat it as transport retry.
        await act.Should().ThrowAsync<Application.Common.Exceptions.NotFoundException>();

        await tx.RollbackAsync();

        await using var verify = _db.CreateContext(SystemTenant());
        var item = await verify.BoardItems.SingleAsync(i => i.Id == graph.ItemId);
        item.GroupId.Should().Be(graph.SourceGroupId, "the rejected mutation must not change Work state");
    }

    [Fact]
    public async Task AutomationMoveItem_ActiveMember_CanonicalPolicyDenies_NoDedupNoMutationNoEvent()
    {
        // The canonical MoveItem decision currently denies an active workspace
        // member over a board-item resource. The public target action must
        // honor that exact decision: no dedup record, no mutation, no event.
        var graph = await SeedChainAsync();
        var (port, workContext) = CreatePort(graph);
        var executionId = Guid.CreateVersion7();

        await using var tx = await workContext.Database.BeginTransactionAsync();
        var act = () => port.MoveItemAsync(
            graph.ItemId, graph.TargetGroupId, executionId,
            new AutomationPrincipal(graph.AccountId, graph.MemberUserId, graph.WorkspaceId),
            CancellationToken.None);

        await act.Should().ThrowAsync<Application.Common.Exceptions.ForbiddenException>(
            "the canonical MoveItem policy governs the public action for members too");

        await tx.RollbackAsync();

        await using var verify = _db.CreateContext(SystemTenant());
        var item = await verify.BoardItems.SingleAsync(i => i.Id == graph.ItemId);
        item.GroupId.Should().Be(graph.SourceGroupId, "no Work mutation for a denied executor");
        item.Version.Should().Be(1, "the denied request never mutated the aggregate");

        (await verify.Set<Notrelix.Infrastructure.Data.Messaging.MessagingOutboxMessage>()
            .IgnoreQueryFilters()
            .AnyAsync(m => m.MessageName == "board_item.moved" && m.WorkspaceId == graph.WorkspaceId))
            .Should().BeFalse("no outward event for a denied executor");

        (await verify.IdempotencyRecords
            .AnyAsync(r => r.KeyHash == Sha256($"move-item:{executionId:N}")))
            .Should().BeFalse("a deny must never begin a dedup record");
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
}
