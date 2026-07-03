using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoard;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Handlers;

[Collection("Database")]
public class GetBoardQueryHandlerTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public GetBoardQueryHandlerTests(PostgresTestContainer db)
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
    public async Task Handle_ShouldReturnBoardDto()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var workspace = Workspace.Create(Guid.NewGuid(), userId, "Test", "test", now);
        context.Workspaces.Add(workspace);

        var board = Board.Create(Guid.NewGuid(), workspace.Id, userId, "My Board", "desc", now, BoardVisibility.Workspace);
        context.Boards.Add(board);
        await context.SaveChangesAsync();

        var handler = new GetBoardQueryHandler(context);

        var result = await handler.Handle(
            new GetBoardQuery(workspace.Id, board.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Title.Should().Be("My Board");
        result.Data.Description.Should().Be("desc");
        result.Data.WorkspaceId.Should().Be(workspace.Id);
    }

    [Fact]
    public async Task Handle_WhenBoardNotFound_ShouldThrowNotFoundException()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);

        var handler = new GetBoardQueryHandler(context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetBoardQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }
}
