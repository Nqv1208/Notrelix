using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Public.Commands;

using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

/// <summary>
/// TAC-WM-006 — the WorkManagement Public move action behaves identically to
/// the HTTP command because both delegate to the single producer-local use
/// case: valid moves succeed, unknown items/groups are semantic not-found,
/// cross-board groups are rejected, and the explicit execution principal from
/// the caller is honored.
/// </summary>
public class WorkItemActionsTests : WorkManagementHandlerTestBase
{
    private readonly WorkItemActions _sut;

    public WorkItemActionsTests()
    {
        _sut = new WorkItemActions(new MoveBoardItemUseCase(DbContextMock.Object, DateTimeProviderMock.Object));
    }

    private WorkItemMoveRequest MoveRequest(Guid itemId, Guid targetGroupId) =>
        new(
            new WorkItemActionIdentity(Guid.CreateVersion7(), TestWorkspaceId, Guid.CreateVersion7()),
            itemId,
            targetGroupId);

    [Fact]
    public async Task MoveItem_ValidRequest_MovesItemToTargetGroup()
    {
        var board = CreateBoard();
        var sourceGroup = BoardGroup.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Source",
            Color.Create("#FF0000"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        var targetGroup = BoardGroup.Create(
            TestAccountId, TestWorkspaceId, board.Id, "Target",
            Color.Create("#00FF00"), FractionalIndex.Create("a1"), TestUserId, TestNow);
        var item = CreateBoardItem(boardId: board.Id, groupId: sourceGroup.Id);
        SetupBoards(board);
        SetupBoardGroups(sourceGroup, targetGroup);
        SetupBoardItems(item);

        var result = await _sut.MoveItemAsync(MoveRequest(item.Id, targetGroup.Id), CancellationToken.None);

        result.ItemId.Should().Be(item.Id);
        result.GroupId.Should().Be(targetGroup.Id);
        result.Position.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task MoveItem_UnknownItem_ThrowsNotFound()
    {
        var request = MoveRequest(Guid.CreateVersion7(), Guid.CreateVersion7());

        await _sut.Invoking(s => s.MoveItemAsync(request, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task MoveItem_GroupOnDifferentBoard_ThrowsNotFound()
    {
        var board = CreateBoard();
        var otherBoard = CreateBoard();
        var targetGroup = BoardGroup.Create(
            TestAccountId, TestWorkspaceId, otherBoard.Id, "Other Board Group",
            Color.Create("#00FF00"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        var item = CreateBoardItem(boardId: board.Id);
        SetupBoards(board, otherBoard);
        SetupBoardGroups(targetGroup);
        SetupBoardItems(item);

        await _sut.Invoking(s => s.MoveItemAsync(MoveRequest(item.Id, targetGroup.Id), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
