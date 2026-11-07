using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardLaneItems;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetFullBoard;
using Notrelix.Application.Features.WorkManagement.Ports.Collaboration;
using Notrelix.Domain.SharedKernel.Ordering;
using BoardGroup = Notrelix.Domain.WorkManagement.BoardGroups.BoardGroup;
using ValidationException = Notrelix.Application.Common.Exceptions.ValidationException;

namespace Notrelix.Application.Tests.Features.WorkManagement.Boards;

/// <summary>
/// A paged board read laid out by another field than Status: a Select field the view groups by becomes the lane
/// field of the page reader (one lane per option), the board and lane reads refuse a field that cannot be a lane,
/// and the search text with the people the client resolved from it reaches the reader.
/// </summary>
public class GetFullBoardGroupByTests : WorkManagementHandlerTestBase
{
    private readonly Mock<IBoardItemPageReader> _reader = new();
    private readonly Mock<IActorLookupService> _actors = new();
    private readonly Mock<IWorkManagementCollaborationReadPort> _collaboration = new();
    private CriteriaField? _laneField;
    private BoardItemCriteria? _criteria;

    public GetFullBoardGroupByTests()
    {
        _actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _collaboration.Setup(c => c.GetCountsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, WorkItemCollaborationCounts>());
        _reader
            .Setup(r => r.ReadAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CriteriaField?>(), It.IsAny<IReadOnlyList<CriteriaField>>(),
                It.IsAny<BoardItemCriteria>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback((Guid _, IReadOnlyList<Guid> _, CriteriaField? lane, IReadOnlyList<CriteriaField> _, BoardItemCriteria criteria, string? _, string? _, int _, CancellationToken _) =>
            {
                _laneField = lane;
                _criteria = criteria;
            })
            .ReturnsAsync((Guid _, IReadOnlyList<Guid> _, CriteriaField? lane, IReadOnlyList<CriteriaField> _, BoardItemCriteria _, string? only, string? _, int _, CancellationToken _) =>
                only is not null
                    ? [new BoardItemLanePage(only, [], 0, 0, null)]
                    : (lane?.OptionOrder.Select(o => o.ToString()) ?? []).Append("").Select(key => new BoardItemLanePage(key, [], 0, 0, null)).ToList());
    }

    private GetFullBoardQueryHandler BoardHandler() => new(
        DbContextMock.Object, _actors.Object,
        new BoardItemSummaryBuilder(DbContextMock.Object, _actors.Object, _collaboration.Object, RequestContextMock.Object),
        _reader.Object, new Notrelix.Testing.Application.Fakes.FakeResourceVisibility(), RequestContextMock.Object);

    private ListBoardLaneItemsQueryHandler LaneHandler() => new(
        DbContextMock.Object,
        new BoardItemSummaryBuilder(DbContextMock.Object, _actors.Object, _collaboration.Object, RequestContextMock.Object),
        _reader.Object);

    private (Board Board, BoardField Status, BoardField Pick, BoardField Note) Seed()
    {
        var board = CreateBoard();
        var group = BoardGroup.Create(TestAccountId, TestWorkspaceId, board.Id, "G", Color.Create("#808080"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        var status = BoardField.Create(TestAccountId, TestWorkspaceId, board.Id, "Status", FieldType.Status,
            FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")!), FractionalIndex.Create("a0"), TestUserId, TestNow);
        status.AddOption("Todo", Color.Create("#111111"), FractionalIndex.Create("a0"), TestUserId, TestNow, WorkStatusCategory.Todo);
        var pick = BoardField.Create(TestAccountId, TestWorkspaceId, board.Id, "Pick", FieldType.Select, FieldSettings.Empty(), FractionalIndex.Create("a1"), TestUserId, TestNow);
        pick.AddOption("Low", Color.Create("#222222"), FractionalIndex.Create("a1"), TestUserId, TestNow);
        pick.AddOption("High", Color.Create("#333333"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        var note = BoardField.Create(TestAccountId, TestWorkspaceId, board.Id, "Note", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a2"), TestUserId, TestNow);
        SetupBoards(board);
        SetupBoardGroups(group);
        SetupBoardFields(status, pick, note);
        SetupBoardItems();
        DbContextMock.Setup(c => c.BoardItemValues).Returns(CreateAsyncDbSet(new List<BoardItemValue>()));
        return (board, status, pick, note);
    }

    [Fact]
    public async Task PagedRead_GroupedByASelectField_LaysThePagesOutByItsOptions()
    {
        var (board, _, pick, _) = Seed();

        var result = await BoardHandler().Handle(new GetFullBoardQuery(board.Id, PageSize: 50, GroupBy: pick.Id.ToString()), CancellationToken.None);

        _laneField!.Id.Should().Be(pick.Id);
        _laneField.OptionOrder.Should().Equal(pick.Options.OrderBy(o => o.Position).Select(o => o.Id), "lanes follow the options' board order");
        result.Data!.Lanes!.Select(l => l.OptionId).Should().Equal(
            pick.Options.OrderBy(o => o.Position).Select(o => (Guid?)o.Id).Append(null));
    }

    [Fact]
    public async Task PagedRead_WithoutAGrouping_OrGroupedByStatus_KeepsTheStatusLanes()
    {
        var (board, status, _, _) = Seed();

        await BoardHandler().Handle(new GetFullBoardQuery(board.Id, PageSize: 50), CancellationToken.None);
        _laneField!.Id.Should().Be(status.Id);

        await BoardHandler().Handle(new GetFullBoardQuery(board.Id, PageSize: 50, GroupBy: status.Id.ToString()), CancellationToken.None);
        _laneField!.Id.Should().Be(status.Id);
    }

    [Theory]
    [InlineData("note")]
    [InlineData("unknown")]
    [InlineData("not-a-guid")]
    public async Task PagedRead_GroupedByAFieldThatCannotBeALane_IsRefusedOnGroupBy(string which)
    {
        var (board, _, _, note) = Seed();
        var groupBy = which switch { "note" => note.Id.ToString(), "unknown" => Guid.NewGuid().ToString(), _ => which };

        var act = () => BoardHandler().Handle(new GetFullBoardQuery(board.Id, PageSize: 50, GroupBy: groupBy), CancellationToken.None);

        (await act.Should().ThrowAsync<ValidationException>()).Which.Errors.Keys.Should().Contain("groupBy");
    }

    [Fact]
    public async Task LanePage_GroupedByASelectField_ReadsThatOptionsLane_AndRefusesAStatusOption()
    {
        var (board, status, pick, _) = Seed();
        var high = pick.Options.Single(o => o.Name == "High").Id;

        var page = await LaneHandler().Handle(new ListBoardLaneItemsQuery(board.Id, high.ToString(), GroupBy: pick.Id.ToString()), CancellationToken.None);
        page.OptionId.Should().Be(high);
        _laneField!.Id.Should().Be(pick.Id);

        var act = () => LaneHandler().Handle(
            new ListBoardLaneItemsQuery(board.Id, status.Options.Single().Id.ToString(), GroupBy: pick.Id.ToString()), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Search_CarriesThePeopleTheClientResolved_ToTheReader()
    {
        var (board, _, _, _) = Seed();
        var person = Guid.NewGuid();

        await BoardHandler().Handle(new GetFullBoardQuery(board.Id, PageSize: 50, Search: "an", SearchMembers: $"{person},{person}"), CancellationToken.None);

        _criteria!.Search.Should().Be("an");
        _criteria.SearchMembers.Should().Equal(person);
    }

    [Fact]
    public void Criteria_SearchMembers_AreIgnoredWithoutASearch_AndRefusedWhenNotIds()
    {
        BoardItemCriteria.Parse(null, null, null, Guid.NewGuid().ToString()).SearchMembers.Should().BeEmpty();
        var act = () => BoardItemCriteria.Parse(null, null, "x", "nope");
        act.Should().Throw<ValidationException>().Which.Errors.Keys.Should().Contain("qMembers");
        var tooMany = string.Join(",", Enumerable.Range(0, BoardItemCriteria.MaxSearchMembers + 1).Select(_ => Guid.NewGuid()));
        var many = () => BoardItemCriteria.Parse(null, null, "x", tooMany);
        many.Should().Throw<ValidationException>().Which.Errors.Keys.Should().Contain("qMembers");
    }
}
