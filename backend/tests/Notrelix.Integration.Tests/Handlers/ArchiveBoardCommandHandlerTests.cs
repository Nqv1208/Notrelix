using Notrelix.Application.Common.Abstractions;
using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.ArchiveBoard;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Integration.Factories;

namespace Notrelix.Integration.Tests.Handlers;

public class ArchiveBoardCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldArchiveBoard()
    {
        using var context = TestDbContextFactory.CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var workspace = Workspace.Create(userId, "Test", "test", now);
        context.Workspaces.Add(workspace);

        var board = Board.Create(workspace.Id, userId, "Board", null, now, BoardVisibility.Workspace);
        context.Boards.Add(board);
        await context.SaveChangesAsync();

        var permissionMock = new Mock<IWorkspacePermissionService>();
        permissionMock.Setup(p => p.EnsureCanManageBoardAsync(board.Id, userId, default))
            .Returns(Task.CompletedTask);

        var handler = new ArchiveBoardCommandHandler(
            context, new FakeCurrentUser { UserId = userId },
            permissionMock.Object, FakeDateTimeProvider.WithFixedTime(now));

        var result = await handler.Handle(new ArchiveBoardCommand(board.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        context.Boards.First(b => b.Id == board.Id).IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenBoardNotFound_ShouldThrowNotFoundException()
    {
        using var context = TestDbContextFactory.CreateInMemoryContext();
        var permissionMock = new Mock<IWorkspacePermissionService>();

        var handler = new ArchiveBoardCommandHandler(
            context, new FakeCurrentUser(),
            permissionMock.Object, FakeDateTimeProvider.WithFixedTime(DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ArchiveBoardCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
