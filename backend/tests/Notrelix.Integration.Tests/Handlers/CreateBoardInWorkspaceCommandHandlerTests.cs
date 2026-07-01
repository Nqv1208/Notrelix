using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.CreateBoardInWorkspace;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Integration.Factories;

namespace Notrelix.Integration.Tests.Handlers;

public class CreateBoardInWorkspaceCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateBoard_WithDefaultFields()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        using var context = TestDbContextFactory.CreateInMemoryContext(currentWorkspace);
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var workspace = Workspace.Create(Guid.NewGuid(), userId, "Test", "test", now);
        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();

        var accessChecker = new TestWorkspaceAccessCheckerStub(true);
        var handler = new CreateBoardInWorkspaceCommandHandler(
            context, new FakeCurrentUser { UserId = userId },
            FakeDateTimeProvider.WithFixedTime(now), accessChecker);

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

        var fields = await context.BoardFields.Where(f => f.BoardId == board.Id).ToListAsync();
        fields.Should().HaveCount(4);
    }

    [Fact]
    public async Task Handle_WhenWorkspaceNotFound_ShouldThrowNotFoundException()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        using var context = TestDbContextFactory.CreateInMemoryContext(currentWorkspace);
        var accessChecker = new TestWorkspaceAccessCheckerStub(false);

        var handler = new CreateBoardInWorkspaceCommandHandler(
            context, new FakeCurrentUser(),
            FakeDateTimeProvider.WithFixedTime(DateTimeOffset.UtcNow), accessChecker);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new CreateBoardInWorkspaceCommand(Guid.NewGuid(), "Board", null, null, null), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldCreateBoard_WithCustomVisibility()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        using var context = TestDbContextFactory.CreateInMemoryContext(currentWorkspace);
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var workspace = Workspace.Create(Guid.NewGuid(), userId, "Test", "test", now);
        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();

        var accessChecker = new TestWorkspaceAccessCheckerStub(true);
        var handler = new CreateBoardInWorkspaceCommandHandler(
            context, new FakeCurrentUser { UserId = userId },
            FakeDateTimeProvider.WithFixedTime(now), accessChecker);

        var result = await handler.Handle(
            new CreateBoardInWorkspaceCommand(workspace.Id, "Private Board", null, null, BoardVisibility.Private),
            CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();
        var board = await context.Boards.FirstOrDefaultAsync(b => b.Id == result.Data);
        board!.Visibility.Should().Be(BoardVisibility.Private);
    }
}
