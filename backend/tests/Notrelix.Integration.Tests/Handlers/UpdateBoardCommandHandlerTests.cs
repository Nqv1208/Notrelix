using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.UpdateBoard;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Integration.Factories;

namespace Notrelix.Integration.Tests.Handlers;

public class UpdateBoardCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldUpdateTitle()
    {
        using var context = TestDbContextFactory.CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var workspace = Workspace.Create(userId, "Test", "test", now);
        context.Workspaces.Add(workspace);

        var board = Board.Create(workspace.Id, userId, "Old Title", null, now, BoardVisibility.Workspace);
        context.Boards.Add(board);
        await context.SaveChangesAsync();

        var handler = new UpdateBoardCommandHandler(
            context, new FakeCurrentUser { UserId = userId },
            FakeDateTimeProvider.WithFixedTime(now));

        var result = await handler.Handle(
            new UpdateBoardCommand(workspace.Id, board.Id, "New Title", null, null, null, null),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        context.Boards.First(b => b.Id == board.Id).Title.Should().Be("New Title");
    }

    [Fact]
    public async Task Handle_WhenBoardNotFound_ShouldThrowNotFoundException()
    {
        using var context = TestDbContextFactory.CreateInMemoryContext();

        var handler = new UpdateBoardCommandHandler(
            context, new FakeCurrentUser(),
            FakeDateTimeProvider.WithFixedTime(DateTimeOffset.UtcNow));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new UpdateBoardCommand(Guid.NewGuid(), Guid.NewGuid(), "Title", null, null, null, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldUpdateDescriptionAndVisibility()
    {
        using var context = TestDbContextFactory.CreateInMemoryContext();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var workspace = Workspace.Create(userId, "Test", "test", now);
        context.Workspaces.Add(workspace);

        var board = Board.Create(workspace.Id, userId, "Board", "old desc", now, BoardVisibility.Private);
        context.Boards.Add(board);
        await context.SaveChangesAsync();

        var handler = new UpdateBoardCommandHandler(
            context, new FakeCurrentUser { UserId = userId },
            FakeDateTimeProvider.WithFixedTime(now));

        var result = await handler.Handle(
            new UpdateBoardCommand(workspace.Id, board.Id, null, "new desc", "blue", BoardVisibility.Workspace, null),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        var updated = context.Boards.First(b => b.Id == board.Id);
        updated.Description.Should().Be("new desc");
        updated.Background.Should().Be("blue");
        updated.Visibility.Should().Be(BoardVisibility.Workspace);
    }
}
