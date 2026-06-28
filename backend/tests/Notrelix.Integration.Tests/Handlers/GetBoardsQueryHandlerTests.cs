using Notrelix.Application.Common.Exceptions;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetBoards;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Integration.Factories;

namespace Notrelix.Integration.Tests.Handlers;

public class GetBoardsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnActiveBoards()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        using var context = TestDbContextFactory.CreateInMemoryContext(currentWorkspace);
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var workspace = Workspace.Create(userId, "Test", "test", now);
        context.Workspaces.Add(workspace);

        var board1 = Board.Create(workspace.Id, userId, "Board 1", null, now, BoardVisibility.Workspace);
        var board2 = Board.Create(workspace.Id, userId, "Board 2", null, now, BoardVisibility.Workspace);
        context.Boards.AddRange(board1, board2);
        await context.SaveChangesAsync();

        var handler = new GetBoardsQueryHandler(context, new TestWorkspaceAccessCheckerStub(true));

        var result = await handler.Handle(new GetBoardsQuery(workspace.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldExcludeArchivedBoards()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        using var context = TestDbContextFactory.CreateInMemoryContext(currentWorkspace);
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var workspace = Workspace.Create(userId, "Test", "test", now);
        context.Workspaces.Add(workspace);

        var active = Board.Create(workspace.Id, userId, "Active", null, now, BoardVisibility.Workspace);
        var archived = Board.Create(workspace.Id, userId, "Archived", null, now, BoardVisibility.Workspace);
        archived.Archive(userId, now);
        context.Boards.AddRange(active, archived);
        await context.SaveChangesAsync();

        var handler = new GetBoardsQueryHandler(context, new TestWorkspaceAccessCheckerStub(true));

        var result = await handler.Handle(new GetBoardsQuery(workspace.Id), CancellationToken.None);

        result.Data.Should().ContainSingle(b => b.Title == "Active");
        result.Data.Should().NotContain(b => b.Title == "Archived");
    }

    [Fact]
    public async Task Handle_WhenWorkspaceNotFound_ShouldThrowNotFoundException()
    {
        var currentWorkspace = new FakeCurrentWorkspace();
        currentWorkspace.EnterSystemContext();
        using var context = TestDbContextFactory.CreateInMemoryContext(currentWorkspace);

        var handler = new GetBoardsQueryHandler(context, new TestWorkspaceAccessCheckerStub(false));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetBoardsQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
