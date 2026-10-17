using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetMyBoardItems;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

/// <summary>
/// WX-BE-005: My Work is a bounded, cursor-paginated, workspace-scoped read of
/// the current user's open assigned Items, ordered by due date (undated last).
/// </summary>
public class GetMyBoardItemsTests : WorkManagementHandlerTestBase
{
    private readonly FakeResourceVisibility _visibility = new();
    private readonly GetMyBoardItemsQueryHandler _handler;

    public GetMyBoardItemsTests()
    {
        _handler = new GetMyBoardItemsQueryHandler(
            DbContextMock.Object,
            RequestContextMock.Object,
            _visibility);
    }

    [Fact]
    public async Task Handle_UserHasAssignedItems_ReturnsItemsWithBoardGroupAndDueDate()
    {
        var board = CreateBoard();
        SetupBoards(board);
        _visibility.Boards.AddRange([board.Id]);

        var groupId = Guid.CreateVersion7();
        var item = CreateBoardItem(boardId: board.Id, groupId: groupId);
        var due = TestNow.AddDays(2);
        item.SetTimeline(null, due, TestUserId, TestNow);
        SetupBoardItems(item);
        SetupBoardItemMembers(CreateBoardItemMember(itemId: item.Id, userId: TestUserId));

        var result = await _handler.Handle(new GetMyBoardItemsQuery(TestWorkspaceId), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Items.Should().ContainSingle();
        var dto = result.Data.Items[0];
        dto.Id.Should().Be(item.Id);
        dto.BoardId.Should().Be(board.Id);
        dto.GroupId.Should().Be(groupId);
        dto.DueAt.Should().Be(due);
        dto.Version.Should().Be(item.Version);
        result.Data.NextCursor.Should().BeNull();
        result.Data.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UserHasNoAssignments_ReturnsEmptyPage()
    {
        var result = await _handler.Handle(new GetMyBoardItemsQuery(TestWorkspaceId), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
        result.Data.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DeletedItemsAreExcluded_ReturnsEmptyPage()
    {
        var board = CreateBoard();
        SetupBoards(board);
        _visibility.Boards.AddRange([board.Id]);

        var item = CreateBoardItem(boardId: board.Id);
        item.Delete(TestUserId, TestNow);
        SetupBoardItems(item);
        SetupBoardItemMembers(CreateBoardItemMember(itemId: item.Id, userId: TestUserId));

        var result = await _handler.Handle(new GetMyBoardItemsQuery(TestWorkspaceId), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CompletedArchivedAndArchivedBoardItemsAreExcluded()
    {
        var board = CreateBoard();
        var archivedBoard = CreateBoard();
        archivedBoard.Archive(TestUserId, TestNow);
        SetupBoards(board, archivedBoard);
        _visibility.Boards.AddRange([board.Id, archivedBoard.Id]);

        var open = CreateBoardItem(boardId: board.Id);
        var completed = CreateBoardItem(boardId: board.Id);
        completed.Complete(TestNow, TestUserId, TestNow);
        var archived = CreateBoardItem(boardId: board.Id);
        archived.Archive(TestUserId, TestNow);
        var onArchivedBoard = CreateBoardItem(boardId: archivedBoard.Id);
        SetupBoardItems(open, completed, archived, onArchivedBoard);
        SetupBoardItemMembers(
            CreateBoardItemMember(itemId: open.Id, userId: TestUserId),
            CreateBoardItemMember(itemId: completed.Id, userId: TestUserId),
            CreateBoardItemMember(itemId: archived.Id, userId: TestUserId),
            CreateBoardItemMember(itemId: onArchivedBoard.Id, userId: TestUserId));

        var result = await _handler.Handle(new GetMyBoardItemsQuery(TestWorkspaceId), CancellationToken.None);

        result.Data!.Items.Select(i => i.Id).Should().Equal(open.Id);
    }

    [Fact]
    public async Task Handle_ItemsAssignedToSomeoneElse_AreExcluded()
    {
        var board = CreateBoard();
        SetupBoards(board);
        _visibility.Boards.AddRange([board.Id]);

        var item = CreateBoardItem(boardId: board.Id);
        SetupBoardItems(item);
        SetupBoardItemMembers(CreateBoardItemMember(itemId: item.Id, userId: Guid.CreateVersion7()));

        var result = await _handler.Handle(new GetMyBoardItemsQuery(TestWorkspaceId), CancellationToken.None);

        result.Data!.Items.Should().BeEmpty("My Work only lists the current user's own assignments");
    }

    [Fact]
    public async Task Handle_DifferentWorkspace_ReturnsEmptyPage()
    {
        var board = CreateBoard();
        SetupBoards(board);
        _visibility.Boards.AddRange([board.Id]);

        var item = CreateBoardItem(boardId: board.Id);
        SetupBoardItems(item);
        SetupBoardItemMembers(CreateBoardItemMember(itemId: item.Id, userId: TestUserId));

        var result = await _handler.Handle(new GetMyBoardItemsQuery(Guid.CreateVersion7()), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_OrdersByDueDateWithUndatedLast_AndPagesWithoutGapsOrDuplicates()
    {
        var board = CreateBoard();
        SetupBoards(board);
        _visibility.Boards.AddRange([board.Id]);

        var undatedA = CreateBoardItem(boardId: board.Id);
        var undatedB = CreateBoardItem(boardId: board.Id);
        var later = CreateBoardItem(boardId: board.Id);
        later.SetTimeline(null, TestNow.AddDays(5), TestUserId, TestNow);
        var soonA = CreateBoardItem(boardId: board.Id);
        soonA.SetTimeline(null, TestNow.AddDays(1), TestUserId, TestNow);
        var soonB = CreateBoardItem(boardId: board.Id);
        soonB.SetTimeline(null, TestNow.AddDays(1), TestUserId, TestNow);
        var overdue = CreateBoardItem(boardId: board.Id);
        overdue.SetTimeline(null, TestNow.AddDays(-3), TestUserId, TestNow);

        var all = new[] { undatedA, undatedB, later, soonA, soonB, overdue };
        SetupBoardItems(all);
        SetupBoardItemMembers(all.Select(i => CreateBoardItemMember(itemId: i.Id, userId: TestUserId)).ToArray());

        var expected = all
            .OrderBy(i => i.DueAt == null)
            .ThenBy(i => i.DueAt)
            .ThenBy(i => i.Id)
            .Select(i => i.Id)
            .ToList();

        var seen = new List<Guid>();
        string? cursor = null;
        var pages = 0;
        do
        {
            var page = (await _handler.Handle(new GetMyBoardItemsQuery(TestWorkspaceId, cursor, 4), CancellationToken.None)).Data!;
            page.Items.Length.Should().BeLessThanOrEqualTo(4);
            seen.AddRange(page.Items.Select(i => i.Id));
            cursor = page.NextCursor;
            pages++;
        }
        while (cursor is not null && pages < 10);

        pages.Should().Be(2, "six items at a page size of four span exactly two pages");
        seen.Should().Equal(expected, "keyset pages must be contiguous: due date ascending, undated last, ties by id");
        seen[0].Should().Be(overdue.Id);
        seen.TakeLast(2).Should().BeEquivalentTo(new[] { undatedA.Id, undatedB.Id });
    }

    [Fact]
    public async Task Handle_DefaultPageSizeIsFifty()
    {
        var board = CreateBoard();
        SetupBoards(board);
        _visibility.Boards.AddRange([board.Id]);

        var items = Enumerable.Range(0, 55).Select(_ => CreateBoardItem(boardId: board.Id)).ToArray();
        SetupBoardItems(items);
        SetupBoardItemMembers(items.Select(i => CreateBoardItemMember(itemId: i.Id, userId: TestUserId)).ToArray());

        var result = await _handler.Handle(new GetMyBoardItemsQuery(TestWorkspaceId), CancellationToken.None);

        result.Data!.Items.Should().HaveCount(GetMyBoardItemsQuery.DefaultLimit);
        result.Data.NextCursor.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validator_RejectsLimitOutsideOneToHundred(int limit)
    {
        var validation = new GetMyBoardItemsQueryValidator().Validate(new GetMyBoardItemsQuery(TestWorkspaceId, null, limit));

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(e => e.PropertyName == nameof(GetMyBoardItemsQuery.Limit));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public void Validator_AcceptsLimitWithinBounds(int limit)
    {
        new GetMyBoardItemsQueryValidator().Validate(new GetMyBoardItemsQuery(TestWorkspaceId, null, limit))
            .IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("not-a-cursor")]
    [InlineData("bjp6eno")]
    public void Validator_RejectsMalformedCursor(string cursor)
    {
        var validation = new GetMyBoardItemsQueryValidator().Validate(new GetMyBoardItemsQuery(TestWorkspaceId, cursor));

        validation.IsValid.Should().BeFalse();
        validation.Errors.Should().Contain(e => e.PropertyName == nameof(GetMyBoardItemsQuery.Cursor));
    }

    [Fact]
    public async Task Handle_ItemOnABoardTheCallerCannotRead_IsNotReturned()
    {
        var visibleBoard = CreateBoard();
        var hiddenBoard = CreateBoard();
        SetupBoards(visibleBoard, hiddenBoard);
        _visibility.Boards.Add(visibleBoard.Id);

        var visibleItem = CreateBoardItem(boardId: visibleBoard.Id, groupId: Guid.CreateVersion7());
        var hiddenItem = CreateBoardItem(boardId: hiddenBoard.Id, groupId: Guid.CreateVersion7());
        SetupBoardItems(visibleItem, hiddenItem);
        SetupBoardItemMembers(
            CreateBoardItemMember(itemId: visibleItem.Id, userId: TestUserId),
            CreateBoardItemMember(itemId: hiddenItem.Id, userId: TestUserId));

        var result = await _handler.Handle(new GetMyBoardItemsQuery(TestWorkspaceId), CancellationToken.None);

        result.Data!.Items.Should().ContainSingle().Which.Id.Should().Be(visibleItem.Id,
            "assigned items on a Board the caller cannot read stay hidden (GOV-029)");
    }
}
