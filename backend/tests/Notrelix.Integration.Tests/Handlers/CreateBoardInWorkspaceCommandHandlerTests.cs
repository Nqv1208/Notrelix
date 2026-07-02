using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardInWorkspace;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Handlers;

[Collection("Database")]
public class CreateBoardInWorkspaceCommandHandlerTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public CreateBoardInWorkspaceCommandHandlerTests(PostgresTestContainer db)
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
    public async Task Handle_ShouldCreateBoard_WithDefaultFields()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        await using var context = _db.CreateContext(currentWorkspace);
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var accountId = Guid.NewGuid();

        var workspace = Workspace.Create(accountId, userId, "Test", "test", now);
        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();

        currentWorkspace.SetWorkspace(accountId, workspace.Id);

        var accessChecker = new TestWorkspaceAccessCheckerStub(true);
        var handler = new CreateBoardInWorkspaceCommandHandler(
            context, new FakeCurrentUser { UserId = userId },
            currentWorkspace, FakeDateTimeProvider.WithFixedTime(now), accessChecker);

        var result = await handler.Handle(
            new CreateBoardInWorkspaceCommand(workspace.Id, "My Board", null, null, null),
            CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();

        var board = await context.Boards.FirstOrDefaultAsync(b => b.Id == result.Data);
        board.Should().NotBeNull();
        board!.Title.Should().Be("My Board");
        board.WorkspaceId.Should().Be(workspace.Id);
        board.AccountId.Should().Be(accountId);

        var fields = await context.BoardFields.Where(f => f.BoardId == board.Id).ToListAsync();
        fields.Should().HaveCount(4);
        fields.Should().AllSatisfy(f => f.AccountId.Should().Be(accountId));
    }

    [Fact]
    public async Task Handle_WhenWorkspaceNotFound_ShouldThrowNotFoundException()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        await using var context = _db.CreateContext(currentWorkspace);
        var accessChecker = new TestWorkspaceAccessCheckerStub(false);

        var handler = new CreateBoardInWorkspaceCommandHandler(
            context, new FakeCurrentUser(),
            currentWorkspace, FakeDateTimeProvider.WithFixedTime(DateTimeOffset.UtcNow), accessChecker);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CreateBoardInWorkspaceCommand(Guid.NewGuid(), "Board", null, null, null), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldCreateBoard_WithCustomVisibility()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        await using var context = _db.CreateContext(currentWorkspace);
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var accountId = Guid.NewGuid();

        var workspace = Workspace.Create(accountId, userId, "Test", "test", now);
        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();

        currentWorkspace.SetWorkspace(accountId, workspace.Id);

        var accessChecker = new TestWorkspaceAccessCheckerStub(true);
        var handler = new CreateBoardInWorkspaceCommandHandler(
            context, new FakeCurrentUser { UserId = userId },
            currentWorkspace, FakeDateTimeProvider.WithFixedTime(now), accessChecker);

        var result = await handler.Handle(
            new CreateBoardInWorkspaceCommand(workspace.Id, "Private Board", null, null, BoardVisibility.Private),
            CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();
        var board = await context.Boards.FirstOrDefaultAsync(b => b.Id == result.Data);
        board.Should().NotBeNull();
        board!.Visibility.Should().Be(BoardVisibility.Private);
    }
}
