using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoard;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Handlers;

[Collection("Database")]
public class UpdateBoardCommandHandlerTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public UpdateBoardCommandHandlerTests(PostgresTestContainer db)
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
    public async Task Handle_ShouldUpdateTitle()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var workspace = Workspace.Create(Guid.NewGuid(), userId, "Test", "test", now);
        context.Workspaces.Add(workspace);

        var board = Board.Create(Guid.NewGuid(), workspace.Id, userId, "Old Title", null, now, BoardVisibility.Workspace);
        context.Boards.Add(board);
        await context.SaveChangesAsync();

        var handler = new UpdateBoardCommandHandler(
            context, new FakeCurrentUser { UserId = userId },
            FakeDateTimeProvider.WithFixedTime(now));

        var result = await handler.Handle(
            new UpdateBoardCommand(board.Id, "New Title", null, null, null, null),
            CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();
        context.Boards.First(b => b.Id == board.Id).Title.Should().Be("New Title");
    }

    [Fact]
    public async Task Handle_WhenBoardNotFound_ShouldThrowNotFoundException()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);

        var handler = new UpdateBoardCommandHandler(
            context, new FakeCurrentUser(),
            FakeDateTimeProvider.WithFixedTime(DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateBoardCommand(Guid.NewGuid(), "Title", null, null, null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldUpdateDescriptionAndVisibility()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var workspace = Workspace.Create(Guid.NewGuid(), userId, "Test", "test", now);
        context.Workspaces.Add(workspace);

        var board = Board.Create(Guid.NewGuid(), workspace.Id, userId, "Board", "old desc", now, BoardVisibility.Private);
        context.Boards.Add(board);
        await context.SaveChangesAsync();

        var handler = new UpdateBoardCommandHandler(
            context, new FakeCurrentUser { UserId = userId },
            FakeDateTimeProvider.WithFixedTime(now));

        var result = await handler.Handle(
            new UpdateBoardCommand(board.Id, null, "new desc", "{\"type\":\"color\",\"value\":\"blue\"}", BoardVisibility.Workspace, null),
            CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();
        var updated = context.Boards.First(b => b.Id == board.Id);
        updated.Description.Should().Be("new desc");
        updated.Background.Should().Be("{\"type\":\"color\",\"value\":\"blue\"}");
        updated.Visibility.Should().Be(BoardVisibility.Workspace);
    }
}
