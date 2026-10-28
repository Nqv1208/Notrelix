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

    private async Task<Guid> CreateAsync(string title, Guid group) =>
        await InTransactionAsync(async context =>
            (await new CreateBoardItemCommandHandler(context, Request(), Clock(), Resolver(context), Keys(context))
                .Handle(new CreateBoardItemCommand(_boardId, group, title), CancellationToken.None)).Id);

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
