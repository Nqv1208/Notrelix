using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoards;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Handlers;

[Collection("Database")]
public class GetBoardsQueryHandlerTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public GetBoardsQueryHandlerTests(PostgresTestContainer db)
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
    public async Task Handle_ShouldReturnActiveBoards()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var workspace = Workspace.Create(Guid.NewGuid(), userId, "Test", "test", now);
        context.Workspaces.Add(workspace);

        var board1 = Board.Create(Guid.NewGuid(), workspace.Id, userId, "Board 1", null, now, BoardVisibility.Workspace);
        var board2 = Board.Create(Guid.NewGuid(), workspace.Id, userId, "Board 2", null, now, BoardVisibility.Workspace);
        context.Boards.AddRange(board1, board2);
        await context.SaveChangesAsync();

        var handler = new GetBoardsQueryHandler(context);

        var result = await handler.Handle(new GetBoardsQuery(workspace.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldExcludeArchivedBoards()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var workspace = Workspace.Create(Guid.NewGuid(), userId, "Test", "test", now);
        context.Workspaces.Add(workspace);

        var active = Board.Create(Guid.NewGuid(), workspace.Id, userId, "Active", null, now, BoardVisibility.Workspace);
        var archived = Board.Create(Guid.NewGuid(), workspace.Id, userId, "Archived", null, now, BoardVisibility.Workspace);
        archived.Archive(userId, now);
        context.Boards.AddRange(active, archived);
        await context.SaveChangesAsync();

        var handler = new GetBoardsQueryHandler(context);

        var result = await handler.Handle(new GetBoardsQuery(workspace.Id), CancellationToken.None);

        result.Data.Should().ContainSingle(b => b.Title == "Active");
        result.Data.Should().NotContain(b => b.Title == "Archived");
    }

    [Fact]
    public async Task Handle_WhenWorkspaceNotFound_ShouldReturnEmpty()
    {
        // Workspace existence is now validated by WorkspaceContextBehavior, not the handler.
        // Handler just queries boards for the given workspace ID.
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);

        var handler = new GetBoardsQueryHandler(context);

        var result = await handler.Handle(new GetBoardsQuery(Guid.NewGuid()), CancellationToken.None);
        result.Succeeded.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }
}
