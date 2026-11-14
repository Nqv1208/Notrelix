using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetFullBoard;
using Notrelix.Application.Features.WorkManagement.Ports.Collaboration;
using Notrelix.Domain.SharedKernel.Ordering;
using BoardGroup = Notrelix.Domain.WorkManagement.BoardGroups.BoardGroup;

namespace Notrelix.Application.Tests.Features.WorkManagement.Boards;

/// <summary>
/// UC-WM-F02: the board read lists its groups by their ordering key, compared by code unit (the
/// column's ordinal collation), whatever order the rows come back in, so every member who reads
/// the board after a move sees the same order.
/// </summary>
public class GetFullBoardGroupOrderTests : WorkManagementHandlerTestBase
{
    private readonly GetFullBoardQueryHandler _handler;

    public GetFullBoardGroupOrderTests()
    {
        var actors = new Mock<IActorLookupService>();
        actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var collaboration = new Mock<IWorkManagementCollaborationReadPort>();
        collaboration.Setup(c => c.GetCountsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, WorkItemCollaborationCounts>());
        _handler = new GetFullBoardQueryHandler(DbContextMock.Object, actors.Object, new Notrelix.Application.Features.WorkManagement.BoardItems.Services.BoardItemSummaryBuilder(DbContextMock.Object, actors.Object, collaboration.Object, RequestContextMock.Object), new Moq.Mock<Notrelix.Application.Features.WorkManagement.BoardItems.Services.IBoardItemPageReader>().Object, new Notrelix.Testing.Application.Fakes.FakeResourceVisibility(), RequestContextMock.Object);
    }

    private BoardGroup Group(Guid boardId, string title, string key) =>
        BoardGroup.Create(TestAccountId, TestWorkspaceId, boardId, title, Color.Create("#808080"), FractionalIndex.Create(key), TestUserId, TestNow);

    private async Task<List<string>> ReadOrder(Board board, IEnumerable<BoardGroup> rows)
    {
        SetupBoards(board);
        SetupBoardGroups(rows.ToArray());
        SetupBoardFields();
        SetupBoardItems();
        DbContextMock.Setup(c => c.BoardItemValues).Returns(CreateAsyncDbSet(new List<BoardItemValue>()));
        var result = await _handler.Handle(new GetFullBoardQuery(board.Id), CancellationToken.None);
        return result.Data!.Lists.Select(list => list.Title).ToList();
    }

    [Fact]
    public async Task Groups_AreListedByKey_ByCodeUnit_WhateverTheRowOrder()
    {
        var board = CreateBoard();
        // "Zz" sorts before "a0" by code unit (uppercase first); a culture compare would put it last.
        var groups = new[]
        {
            Group(board.Id, "Kiểm thử", "a1"),
            Group(board.Id, "Go-live", "a2"),
            Group(board.Id, "Chuẩn bị", "Zz"),
            Group(board.Id, "Dữ liệu", "a0V"),
        };
        var want = new[] { "Chuẩn bị", "Dữ liệu", "Kiểm thử", "Go-live" };

        (await ReadOrder(board, groups)).Should().Equal(want);
        (await ReadOrder(board, groups.Reverse())).Should().Equal(want);
        (await ReadOrder(board, groups.Skip(2).Concat(groups.Take(2)))).Should().Equal(want);
    }

    [Fact]
    public async Task AMovedGroup_IsReadAtItsNewPlace_ByEveryReader()
    {
        var board = CreateBoard();
        var first = Group(board.Id, "A", "a0");
        var second = Group(board.Id, "B", "a1");
        var third = Group(board.Id, "C", "a2");
        // B moves in front of A: the server gives it a key before A's.
        second.UpdatePosition(FractionalIndex.Create("Zz"), TestUserId, TestNow);

        (await ReadOrder(board, [first, second, third])).Should().Equal("B", "A", "C");
        (await ReadOrder(board, [third, first, second])).Should().Equal("B", "A", "C");
    }
}
