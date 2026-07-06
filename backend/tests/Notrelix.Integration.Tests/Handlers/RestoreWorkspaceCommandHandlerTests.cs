using Notrelix.Application.Features.Workspaces.Workspaces.Commands.RestoreWorkspace;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Handlers;

[Collection("Database")]
public class RestoreWorkspaceCommandHandlerTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public RestoreWorkspaceCommandHandlerTests(PostgresTestContainer db)
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
    public async Task Handle_WhenWorkspaceIsSoftDeleted_ShouldRestore()
    {
        await using var context = _db.CreateContext();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var workspace = Workspace.Create(Guid.NewGuid(), userId, "Test", "test", now);
        workspace.SoftDelete(userId, now);
        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();

        var handler = new RestoreWorkspaceCommandHandler(context, new FakeCurrentUser { UserId = userId }, FakeDateTimeProvider.WithFixedTime(now));

        var result = await handler.Handle(new RestoreWorkspaceCommand(workspace.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        await context.SaveChangesAsync();
        context.Workspaces.First(w => w.Id == workspace.Id).Status.Should().Be(WorkspaceStatus.Active);
    }

    [Fact]
    public async Task Handle_WhenWorkspaceNotFound_ShouldThrowNotFoundException()
    {
        await using var context = _db.CreateContext();
        var userId = Guid.NewGuid();

        var handler = new RestoreWorkspaceCommandHandler(context, new FakeCurrentUser { UserId = userId }, FakeDateTimeProvider.WithFixedTime(DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new RestoreWorkspaceCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenWorkspaceIsActive_ShouldBeNoOp()
    {
        await using var context = _db.CreateContext();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var workspace = Workspace.Create(Guid.NewGuid(), userId, "Test", "test", now);
        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();

        var handler = new RestoreWorkspaceCommandHandler(context, new FakeCurrentUser { UserId = userId }, FakeDateTimeProvider.WithFixedTime(now));

        var result = await handler.Handle(new RestoreWorkspaceCommand(workspace.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        await context.SaveChangesAsync();
        context.Workspaces.First(w => w.Id == workspace.Id).Status.Should().Be(WorkspaceStatus.Active);
    }
}
