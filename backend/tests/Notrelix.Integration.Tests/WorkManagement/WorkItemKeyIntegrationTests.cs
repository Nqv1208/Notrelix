using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.CreateBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Ordering;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.WorkManagement;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// Item keys on PostgreSQL: creates running concurrently, each in its own request
/// transaction, draw distinct consecutive keys from the Board sequence (the row lock
/// of the reserving UPDATE serializes them), the Board sequence ends at the highest
/// key, the Board version is untouched so a Board edit made with the version read
/// before the creates still succeeds, and a moved Item keeps its key. A duplicate
/// key on one Board is rejected by the database as a last line of defence.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkItemKeyIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 0, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;
    private Guid _accountId;
    private Guid _workspaceId;
    private Guid _userId;
    private Guid _boardId;
    private Guid _todoId;
    private Guid _doneId;

    public WorkItemKeyIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.OwnerConnectionString);
        await _reset.ResetAsync();

        _accountId = Guid.NewGuid();
        _userId = Guid.NewGuid();
        var workspace = Workspace.Create(_accountId, _userId, "Keys WS", $"keys-{Guid.NewGuid():N}", Now);
        var board = Board.Create(_accountId, workspace.Id, _userId, "Keys Board", null, Now, itemKeyPrefix: "WEB");
        var todo = BoardGroup.Create(_accountId, workspace.Id, board.Id, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), _userId, Now);
        var done = BoardGroup.Create(_accountId, workspace.Id, board.Id, "Done", Color.Create("#808080"), FractionalIndex.Create("a1"), _userId, Now);
        (_workspaceId, _boardId, _todoId, _doneId) = (workspace.Id, board.Id, todo.Id, done.Id);

        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Workspaces.Add(workspace);
        seed.Boards.Add(board);
        seed.BoardGroups.AddRange(todo, done);
        await seed.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ConcurrentCreates_GetDistinctConsecutiveKeys()
    {
        const int creates = 8;
        var boardVersionBefore = await BoardScalarAsync(b => b.Version);

        var ids = await Task.WhenAll(Enumerable.Range(0, creates)
            .Select(i => Task.Run(() => CreateAsync($"item {i}", i % 2 == 0 ? _todoId : _doneId))));

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        var keys = await verify.BoardItems.Where(i => ids.Contains(i.Id)).Select(i => new { i.ItemKey, i.ItemSequence }).ToListAsync();
        keys.Select(k => k.ItemKey).Should().BeEquivalentTo(Enumerable.Range(1, creates).Select(n => $"WEB-{n}"));
        keys.Select(k => k.ItemSequence).Should().OnlyHaveUniqueItems();
        (await BoardScalarAsync(b => b.ItemSequence)).Should().Be(creates);
        (await BoardScalarAsync(b => b.Version)).Should().Be(boardVersionBefore, "reserving keys does not version the Board");
    }

    [Fact]
    public async Task ABoardRename_WithTheVersionReadBeforeItemCreates_Succeeds()
    {
        await using var editor = _db.CreateOwnerContext(Tenant());
        var board = await editor.Boards.SingleAsync(b => b.Id == _boardId);
        var versionRead = board.Version;

        for (var i = 0; i < 3; i++)
            await CreateAsync($"item {i}", _todoId);

        await using var transaction = await editor.Database.BeginTransactionAsync();
        board.Rename("Renamed Board", _userId, Now.AddMinutes(1));
        await editor.SaveChangesAsync();
        await transaction.CommitAsync();

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        var saved = await verify.Boards.SingleAsync(b => b.Id == _boardId);
        saved.Title.Should().Be("Renamed Board");
        saved.Version.Should().Be(versionRead + 1);
        saved.ItemSequence.Should().Be(3, "the rename must not write back the sequence it read before the creates");
    }

    [Fact]
    public async Task MovingAnItem_KeepsItsKey()
    {
        var item = await CreateAsync("mover", _todoId);

        await InTransactionAsync(async context =>
        {
            var currentUser = new Mock<ICurrentUser>();
            currentUser.Setup(u => u.UserId).Returns(_userId);
            var handler = new MoveBoardItemCommandHandler(new MoveBoardItemUseCase(context, Clock(), Resolver(context)), currentUser.Object);
            await handler.Handle(new MoveBoardItemCommand(item, _doneId, 1, null, null), CancellationToken.None);
            return true;
        });

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        var moved = await verify.BoardItems.SingleAsync(i => i.Id == item);
        moved.GroupId.Should().Be(_doneId);
        moved.ItemKey.Should().Be("WEB-1");
    }

    [Fact]
    public async Task ADuplicateKeyOnOneBoard_IsRejectedByTheDatabase()
    {
        await CreateAsync("first", _todoId);

        var act = async () =>
        {
            await using var context = _db.CreateOwnerContext(SystemTenant());
            context.BoardItems.Add(BoardItem.CreateRoot(_accountId, _workspaceId, _boardId, _todoId, "clash",
                FractionalIndex.Create("a9"), _userId, Now, itemKey: "WEB-1", itemSequence: 1));
            await context.SaveChangesAsync();
        };

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task TwentyParallelCreates_OnOneBoard_NeverShareAKey()
    {
        // UC-WM-A10 acceptance: 20 creates at once, each in its own request transaction, draw 20 distinct numbers.
        const int creates = 20;

        var ids = await Task.WhenAll(Enumerable.Range(0, creates).Select(i => Task.Run(() => CreateAsync($"item {i}", _todoId))));

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        var rows = await verify.BoardItems.Where(i => ids.Contains(i.Id)).Select(i => new { i.ItemKey, i.ItemSequence }).ToListAsync();
        rows.Should().HaveCount(creates);
        rows.Select(r => r.ItemKey).Should().OnlyHaveUniqueItems();
        rows.Select(r => r.ItemSequence).Should().BeEquivalentTo(Enumerable.Range(1, creates).Select(n => (long)n));
        (await BoardScalarAsync(b => b.ItemSequence)).Should().Be(creates);
    }

    [Fact]
    public async Task TenConcurrentInserts_AtOnePosition_GetDistinctKeysAndADeterministicOrder()
    {
        // UC-WM-F04: ten creates aimed between the same two neighbours of one group, all at once. The group's ordering
        // lock serializes them, so each one reads the committed keys and none collides; the order they end in is fixed
        // by the stored keys alone (read twice, it is the same), and every one of them lies between the two neighbours.
        var first = await CreateAsync("first", _todoId);
        var last = await CreateAsync("last", _todoId);

        // Ten appends to the bottom of the group: all land, each after the one committed before it.
        var appended = await Task.WhenAll(Enumerable.Range(0, 10).Select(i => Task.Run(() => CreateAsync($"append {i}", _todoId))));

        // Ten inserts between the same two neighbours: the first to take the lock lands between them; the others
        // find the neighbours no longer adjacent and get the stable ordering conflict, never a shared key.
        var between = await Task.WhenAll(Enumerable.Range(0, 10).Select(i => Task.Run(async () =>
        {
            try { return (Guid?)await CreateAsync($"between {i}", _todoId, previous: first, next: last); }
            catch (Notrelix.Application.Common.Exceptions.ConflictException conflict)
            {
                conflict.ErrorCode.Should().Be(WorkOrderingErrorCodes.StalePlacement);
                return null;
            }
        })));
        var landed = between.Where(id => id is not null).Select(id => id!.Value).ToList();
        landed.Should().ContainSingle("only one insert can be between two neighbours that were adjacent when it was asked");

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        var rows = await verify.BoardItems.Where(i => i.GroupId == _todoId).Select(i => new { i.Id, i.Position }).ToListAsync();
        rows.Should().HaveCount(13);
        rows.Select(r => r.Position.Value).Should().OnlyHaveUniqueItems("a real collision is a stable conflict, not a shared key");
        var order = rows.OrderBy(r => r.Position).Select(r => r.Id).ToList();
        order.Take(3).Should().Equal(first, landed[0], last);
        order.Skip(3).Should().BeEquivalentTo(appended);

        await using var again = _db.CreateOwnerContext(SystemTenant());
        (await again.BoardItems.Where(i => i.GroupId == _todoId).OrderBy(i => i.Position).Select(i => i.Id).ToListAsync())
            .Should().Equal(order, "the order is the stored keys' order, the same on every read");
    }

    [Fact]
    public async Task AMovedItemsOldKey_StillResolvesToTheItem_OnlyForSomeoneWhoSeesItsNewBoard()
    {
        // UC-WM-A10 / D76.3: the item moved to board B and took B's key; its old key on this board is a stored alias.
        var other = Board.Create(_accountId, _workspaceId, _userId, "Target", null, Now, itemKeyPrefix: "OPS");
        var group = BoardGroup.Create(_accountId, _workspaceId, other.Id, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), _userId, Now);
        var moved = BoardItem.CreateRoot(_accountId, _workspaceId, other.Id, group.Id, "moved", FractionalIndex.Create("a0"), _userId, Now, itemKey: "OPS-9", itemSequence: 9);
        await using (var seed = _db.CreateOwnerContext(SystemTenant()))
        {
            seed.Boards.Add(other);
            seed.BoardGroups.Add(group);
            seed.BoardItems.Add(moved);
            seed.BoardItemKeyAliases.Add(BoardItemKeyAlias.Create(_accountId, _workspaceId, _boardId, moved.Id, "WEB-3"));
            await seed.SaveChangesAsync();
        }
        var stays = await CreateAsync("stays", _todoId);

        var byOldKey = await ResolveAsync("WEB-3", seesBoards: [_boardId, other.Id]);
        byOldKey.Should().Be(new Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ResolveBoardItemKey.ResolvedBoardItemKeyDto(moved.Id, other.Id, "OPS-9", true));
        (await ResolveAsync("WEB-1", seesBoards: [_boardId])).ItemId.Should().Be(stays);

        var hidden = () => ResolveAsync("WEB-3", seesBoards: [_boardId]);
        await hidden.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.NotFoundException>("the new board is not one the caller sees");
        var never = () => ResolveAsync("WEB-77", seesBoards: [_boardId, other.Id]);
        await never.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.NotFoundException>();
    }

    private async Task<Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ResolveBoardItemKey.ResolvedBoardItemKeyDto> ResolveAsync(string key, Guid[] seesBoards)
    {
        await using var context = _db.CreateOwnerContext(Tenant());
        var visibility = new Mock<Notrelix.Application.Features.Governance.Public.Visibility.IResourceVisibility>();
        visibility.Setup(v => v.VisibleBoardIdsAsync(_workspaceId, _userId, It.IsAny<CancellationToken>())).ReturnsAsync(seesBoards);
        return await new Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ResolveBoardItemKey.ResolveBoardItemKeyQueryHandler(context, Request(), visibility.Object, new PostgresBoardItemKeyAliases(context))
            .Handle(new Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ResolveBoardItemKey.ResolveBoardItemKeyQuery(_boardId, key), CancellationToken.None);
    }

    private async Task<Guid> CreateAsync(string title, Guid group, Guid? previous = null, Guid? next = null) =>
        await InTransactionAsync(async context =>
            (await new CreateBoardItemCommandHandler(context, Request(), Clock(), Resolver(context), Keys(context))
                .Handle(new CreateBoardItemCommand(_boardId, group, title, previous, next), CancellationToken.None)).Id);

    private async Task<long> BoardScalarAsync(System.Linq.Expressions.Expression<Func<Board, long>> selector)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        return await context.Boards.Where(b => b.Id == _boardId).Select(selector).SingleAsync();
    }

    private async Task<T> InTransactionAsync<T>(Func<ApplicationDbContext, Task<T>> work)
    {
        await using var context = _db.CreateOwnerContext(Tenant());
        await using var transaction = await context.Database.BeginTransactionAsync();
        var result = await work(context);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return result;
    }

    private static WorkPlacementResolver Resolver(ApplicationDbContext context) => new(new PostgresWorkOrderingScopeLock(context));

    private static IBoardItemKeyAllocator Keys(ApplicationDbContext context) => new PostgresBoardItemKeyAllocator(context);

    private static IDateTimeProvider Clock() => FakeDateTimeProvider.WithFixedTime(Now);

    private FakeCurrentRequestContext Request()
    {
        var request = new FakeCurrentRequestContext().AsUser(_userId);
        request.Tenant.SetWorkspace(_accountId, _workspaceId, _userId);
        return request;
    }

    private ICurrentTenantContext Tenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_accountId, _workspaceId, _userId);
        return tenant;
    }

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }
}
