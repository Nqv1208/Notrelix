using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.RestoreBoardItem;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.RestoreBoard;
using Notrelix.Application.Features.WorkManagement.Ordering;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data.WorkManagement;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// LIFE-RESTORE-001 / LIFE-GUARD-001 (WM-V3-LIFE-001). Soft-delete lifecycle must
/// hold against state reloaded from PostgreSQL: every step runs in a fresh
/// DbContext under a real (non-system) tenant scope, so neither the global
/// soft-delete filter nor an in-memory tracked instance can mask the result.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkSoftDeleteLifecycleIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public WorkSoftDeleteLifecycleIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.OwnerConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task RestoreBoardItem_FromFreshRequest_RestoresDeletedItemOnce()
    {
        var stack = await SeedBoardStackAsync(deleteItem: true);
        var versionBeforeRestore = await ReadItemVersionAsync(stack);

        await RestoreItemAsync(stack, stack.ItemId);

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        var item = await verify.BoardItems.IgnoreQueryFilters().SingleAsync(i => i.Id == stack.ItemId);
        item.DeletedAt.Should().BeNull();
        item.IsDeleted.Should().BeFalse();
        item.Version.Should().Be(versionBeforeRestore + 1, "an accepted restore increments version exactly once");
    }

    [Fact]
    public async Task RestoreBoard_FromFreshRequest_RestoresDeletedBoard()
    {
        var stack = await SeedBoardStackAsync(deleteBoard: true);

        await using (var context = _db.CreateOwnerContext(stack.Request.Tenant))
        {
            var handler = new RestoreBoardCommandHandler(context, stack.Request, FakeDateTimeProvider.WithFixedTime(Now.AddMinutes(1)));
            await handler.Handle(new RestoreBoardCommand(stack.BoardId, ExpectedVersion: 1), CancellationToken.None);
            await context.SaveChangesAsync();
        }

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        var board = await verify.Boards.IgnoreQueryFilters().SingleAsync(b => b.Id == stack.BoardId);
        board.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task RestoreBoardItem_OfActiveItem_IsSemanticNoOp()
    {
        var stack = await SeedBoardStackAsync(deleteItem: false);
        var versionBefore = await ReadItemVersionAsync(stack);

        await RestoreItemAsync(stack, stack.ItemId);

        (await ReadItemVersionAsync(stack)).Should().Be(versionBefore, "restoring an active item changes nothing");
    }

    [Fact]
    public async Task RestoreBoardItem_FromForeignTenant_IsNotFound_AndItemStaysDeleted()
    {
        var foreign = await SeedBoardStackAsync(deleteItem: true);
        var caller = await SeedBoardStackAsync(deleteItem: false);

        var act = () => RestoreItemAsync(caller, foreign.ItemId);

        await act.Should().ThrowAsync<NotFoundException>(
            "bypassing the soft-delete filter must not bypass the tenant predicate");
        await using var verify = _db.CreateOwnerContext(SystemTenant());
        var item = await verify.BoardItems.IgnoreQueryFilters().SingleAsync(i => i.Id == foreign.ItemId);
        item.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ReloadedDeletedItem_ReportsDeleted()
    {
        var stack = await SeedBoardStackAsync(deleteItem: true);

        await using var context = _db.CreateOwnerContext(SystemTenant());
        var item = await context.BoardItems.IgnoreQueryFilters().SingleAsync(i => i.Id == stack.ItemId);

        item.IsDeleted.Should().BeTrue("the lifecycle flag must be hydrated from the persisted deleted_at column");
    }

    private async Task RestoreItemAsync(BoardStack caller, Guid itemId)
    {
        await using var context = _db.CreateOwnerContext(caller.Request.Tenant);
        await using var transaction = await context.Database.BeginTransactionAsync();
        var handler = new RestoreBoardItemCommandHandler(
            context, caller.Request, FakeDateTimeProvider.WithFixedTime(Now.AddMinutes(1)),
            new WorkPlacementResolver(new PostgresWorkOrderingScopeLock(context)));
        await handler.Handle(new RestoreBoardItemCommand(itemId), CancellationToken.None);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private async Task<long> ReadItemVersionAsync(BoardStack stack)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        return await context.BoardItems.IgnoreQueryFilters()
            .Where(i => i.Id == stack.ItemId)
            .Select(i => i.Version)
            .SingleAsync();
    }

    private sealed record BoardStack(Guid BoardId, Guid ItemId, FakeCurrentRequestContext Request);

    private async Task<BoardStack> SeedBoardStackAsync(bool deleteItem = false, bool deleteBoard = false)
    {
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create(accountId, ownerId, "Lifecycle WS", $"life-{Guid.NewGuid():N}", Now);
        var board = Board.Create(accountId, workspace.Id, ownerId, "Lifecycle Board", null, Now);
        var group = BoardGroup.Create(
            accountId, workspace.Id, board.Id, "Todo",
            Color.Create("#808080"), FractionalIndex.Create("a0"), ownerId, Now);
        var item = BoardItem.CreateRoot(
            accountId, workspace.Id, board.Id, group.Id, "Task", FractionalIndex.Create("a0"), ownerId, Now);
        if (deleteItem) item.Delete(ownerId, Now);
        if (deleteBoard) board.Delete(ownerId, Now);

        await using (var seed = _db.CreateOwnerContext(SystemTenant()))
        {
            seed.Workspaces.Add(workspace);
            seed.Boards.Add(board);
            seed.BoardGroups.Add(group);
            seed.BoardItems.Add(item);
            await seed.SaveChangesAsync();
        }

        var request = new FakeCurrentRequestContext().AsUser(ownerId);
        request.Tenant.SetWorkspace(accountId, workspace.Id, ownerId);
        return new BoardStack(board.Id, item.Id, request);
    }

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }
}
