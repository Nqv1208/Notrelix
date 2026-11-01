using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetFullBoard;
using Notrelix.Application.Features.WorkManagement.Ports.Collaboration;
using Notrelix.Domain.SharedKernel.Ordering;
using BoardGroup = Notrelix.Domain.WorkManagement.BoardGroups.BoardGroup;

namespace Notrelix.Application.Tests.Features.WorkManagement.Boards;

/// <summary>
/// Board settings read: each Select/Status option carries the number of live
/// (not archived, not trashed) Items of this Board that hold it.
/// </summary>
public class GetFullBoardOptionItemCountTests : WorkManagementHandlerTestBase
{
    private readonly GetFullBoardQueryHandler _handler;

    public GetFullBoardOptionItemCountTests()
    {
        var actors = new Mock<IActorLookupService>();
        actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var collaboration = new Mock<IWorkManagementCollaborationReadPort>();
        collaboration.Setup(c => c.GetCountsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, WorkItemCollaborationCounts>());
        _handler = new GetFullBoardQueryHandler(DbContextMock.Object, actors.Object, new Notrelix.Application.Features.WorkManagement.BoardItems.Services.BoardItemSummaryBuilder(DbContextMock.Object, actors.Object, collaboration.Object, RequestContextMock.Object), new Moq.Mock<Notrelix.Application.Features.WorkManagement.BoardItems.Services.IBoardItemPageReader>().Object, new Notrelix.Testing.Application.Fakes.FakeResourceVisibility(), RequestContextMock.Object);
    }

    private BoardField StatusField(Guid boardId)
    {
        var field = BoardField.Create(
            TestAccountId, TestWorkspaceId, boardId, "Status", FieldType.Status,
            FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")!), FractionalIndex.Create("a0"), TestUserId, TestNow);
        field.AddOption("Todo", Color.Create("#111111"), FractionalIndex.Create("a0"), TestUserId, TestNow, WorkStatusCategory.Todo);
        field.AddOption("Done", Color.Create("#222222"), FractionalIndex.Create("a1"), TestUserId, TestNow, WorkStatusCategory.Done);
        return field;
    }

    private BoardItem ItemWith(Board board, BoardGroup group, BoardField field, FieldOption option)
    {
        var item = CreateBoardItem(boardId: board.Id, groupId: group.Id);
        item.UpdateFieldValue(field, FieldValue.Create(JsonValue.Create($"\"{option.Id}\"")!), TestUserId, TestNow);
        return item;
    }

    [Fact]
    public async Task OptionCount_CountsLiveItemsOnly_AndIgnoresOtherBoards()
    {
        var board = CreateBoard();
        var group = BoardGroup.Create(TestAccountId, TestWorkspaceId, board.Id, "G", Color.Create("#808080"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        var status = StatusField(board.Id);
        var todo = status.Options.First(o => o.Name == "Todo");
        var done = status.Options.First(o => o.Name == "Done");

        var live1 = ItemWith(board, group, status, todo);
        var live2 = ItemWith(board, group, status, todo);
        var archived = ItemWith(board, group, status, todo);
        archived.Archive(TestUserId, TestNow);
        var trashed = ItemWith(board, group, status, todo);
        trashed.Delete(TestUserId, TestNow);
        var doneItem = ItemWith(board, group, status, done);
        // Another Board's Item holding that Board's own Todo option must not leak into this Board's counts.
        var otherBoard = CreateBoard();
        var otherStatus = StatusField(otherBoard.Id);
        var otherBoardItem = ItemWith(otherBoard, group, otherStatus, otherStatus.Options.First(o => o.Name == "Todo"));
        var items = new[] { live1, live2, archived, trashed, doneItem, otherBoardItem };

        SetupBoards(board);
        SetupBoardGroups(group);
        SetupBoardFields(status);
        SetupBoardItems(items);
        DbContextMock.Setup(c => c.BoardItemValues).Returns(CreateAsyncDbSet(items.SelectMany(i => i.FieldValues).ToList()));

        var result = await _handler.Handle(new GetFullBoardQuery(board.Id), CancellationToken.None);

        var options = result.Data!.Columns.Single(c => c.Id == status.Id).Options!;
        options.Single(o => o.Id == todo.Id).ItemCount.Should().Be(2);
        options.Single(o => o.Id == done.Id).ItemCount.Should().Be(1);
    }
}
