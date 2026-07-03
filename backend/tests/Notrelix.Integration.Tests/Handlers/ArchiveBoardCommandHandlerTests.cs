using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.ArchiveBoard;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Handlers;

[Collection("Database")]
public class ArchiveBoardCommandHandlerTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public ArchiveBoardCommandHandlerTests(PostgresTestContainer db)
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
    public async Task Handle_ShouldArchiveBoard()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var workspace = Workspace.Create(Guid.NewGuid(), userId, "Test", "test", now);
        context.Workspaces.Add(workspace);

        var board = Board.Create(Guid.NewGuid(), workspace.Id, userId, "Board", null, now, BoardVisibility.Workspace);
        context.Boards.Add(board);
        await context.SaveChangesAsync();

        var permissionMock = new Mock<IWorkspacePermissionService>();
        permissionMock.Setup(p => p.EnsureCanManageBoardAsync(board.Id, userId, default))
            .Returns(Task.CompletedTask);

        var handler = new ArchiveBoardCommandHandler(
            context, new FakeCurrentUser { UserId = userId },
            permissionMock.Object, FakeDateTimeProvider.WithFixedTime(now));

        var result = await handler.Handle(new ArchiveBoardCommand(board.Id), CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();
        context.Boards.First(b => b.Id == board.Id).IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenBoardNotFound_ShouldThrowNotFoundException()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = _db.CreateContext(tenant);
        var permissionMock = new Mock<IWorkspacePermissionService>();

        var handler = new ArchiveBoardCommandHandler(
            context, new FakeCurrentUser(),
            permissionMock.Object, FakeDateTimeProvider.WithFixedTime(DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ArchiveBoardCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
