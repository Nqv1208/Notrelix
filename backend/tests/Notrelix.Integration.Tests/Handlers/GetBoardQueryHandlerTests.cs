using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoard;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Integration.Factories;

namespace Notrelix.Integration.Tests.Handlers;

public class GetBoardQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnBoardDto()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        using var context = TestDbContextFactory.CreateInMemoryContext(currentWorkspace);
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
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        using var context = TestDbContextFactory.CreateInMemoryContext(currentWorkspace);

        var handler = new GetBoardQueryHandler(context);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetBoardQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }
}
