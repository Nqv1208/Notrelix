using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UnarchiveBoard;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Integration.Factories;

namespace Notrelix.Integration.Tests.Handlers;

public class UnarchiveBoardCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldUnarchiveBoard()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        using var context = TestDbContextFactory.CreateInMemoryContext(currentWorkspace);
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var workspace = Workspace.Create(userId, "Test", "test", now);
        context.Workspaces.Add(workspace);

        var board = Board.Create(workspace.Id, userId, "Board", null, now, BoardVisibility.Workspace);
        board.Archive(userId, now);
        context.Boards.Add(board);
        await context.SaveChangesAsync();

        var permissionMock = new Mock<IWorkspacePermissionService>();
        permissionMock.Setup(p => p.EnsureCanManageBoardAsync(board.Id, userId, default))
            .Returns(Task.CompletedTask);

        var handler = new UnarchiveBoardCommandHandler(
            context, new FakeCurrentUser { UserId = userId },
            permissionMock.Object, FakeDateTimeProvider.WithFixedTime(now));

        var result = await handler.Handle(new UnarchiveBoardCommand(board.Id), CancellationToken.None);
        await context.SaveChangesAsync();

        result.Succeeded.Should().BeTrue();
        context.Boards.First(b => b.Id == board.Id).IsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenBoardNotFound_ShouldThrowNotFoundException()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        using var context = TestDbContextFactory.CreateInMemoryContext(currentWorkspace);
        var permissionMock = new Mock<IWorkspacePermissionService>();

        var handler = new UnarchiveBoardCommandHandler(
            context, new FakeCurrentUser(),
            permissionMock.Object, FakeDateTimeProvider.WithFixedTime(DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UnarchiveBoardCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
