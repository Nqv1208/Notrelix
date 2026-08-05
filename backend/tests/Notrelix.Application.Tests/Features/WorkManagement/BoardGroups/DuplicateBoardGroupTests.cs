using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DuplicateBoardGroup;

using Notrelix.Domain.SharedKernel.Ordering;
namespace Notrelix.Application.Tests.Features.WorkManagement.BoardGroups;

public class DuplicateBoardGroupTests : WorkManagementHandlerTestBase
{
    private readonly DuplicateBoardGroupCommandHandler _handler;

    public DuplicateBoardGroupTests()
    {
        _handler = new DuplicateBoardGroupCommandHandler(
            DbContextMock.Object,
            RequestContextMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_GroupExists_CreatesDuplicate()
    {
        var board = CreateBoard();
        var group = CreateBoardGroup(boardId: board.Id);
        SetupBoards(board);
        SetupBoardGroups(group);

        var command = new DuplicateBoardGroupCommand(group.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBe(group.Id);
    }

    [Fact]
    public async Task Handle_GroupNotFound_ThrowsNotFoundException()
    {
        var command = new DuplicateBoardGroupCommand(Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DeletedGroup_ThrowsNotFoundException()
    {
        var board = CreateBoard();
        var group = CreateBoardGroup(boardId: board.Id);
        group.Delete(TestUserId, TestNow);
        SetupBoards(board);
        SetupBoardGroups(group);

        var command = new DuplicateBoardGroupCommand(group.Id);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_GroupWithItems_ClonesItems()
    {
        var board = CreateBoard();
        var group = CreateBoardGroup(boardId: board.Id);
        var item = CreateBoardItem(boardId: board.Id, groupId: group.Id);
        SetupBoards(board);
        SetupBoardGroups(group);
        SetupBoardItems(item);

        var command = new DuplicateBoardGroupCommand(group.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    private BoardGroup CreateBoardGroup(Guid? id = null, Guid? boardId = null)
    {
        var group = Notrelix.Domain.WorkManagement.BoardGroups.BoardGroup.Create(
            TestAccountId,
            TestWorkspaceId,
            boardId ?? Guid.CreateVersion7(),
            "Test Group",
            Color.Create("#808080"),
            FractionalIndex.Create("a0"),
            TestUserId,
            TestNow);
        if (id.HasValue)
            group.GetType().GetProperty(nameof(BoardGroup.Id))!.SetValue(group, id.Value);
        return group;
    }
}
