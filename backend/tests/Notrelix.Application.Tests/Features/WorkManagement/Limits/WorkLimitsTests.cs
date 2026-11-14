using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.CreateBoardField;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.CreateBoardGroup;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.CreateBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnarchiveBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardViews.Commands.CreateBoardView;
using Notrelix.Application.Features.WorkManagement.Limits;
using Notrelix.Domain.SharedKernel.Ordering;
using AppConflict = Notrelix.Application.Common.Exceptions.ConflictException;

namespace Notrelix.Application.Tests.Features.WorkManagement.Limits;

/// <summary>
/// The published limits of a board (D78.8, UC-WM-T03): the write that would go over one is refused whole with the
/// limit's code and its current and max; the write at the limit itself still goes through.
/// </summary>
public class WorkLimitsTests : WorkManagementHandlerTestBase
{
    private readonly Board _board;
    private readonly BoardGroup _group;

    public WorkLimitsTests()
    {
        _board = CreateBoard();
        _group = BoardGroup.Create(TestAccountId, TestWorkspaceId, _board.Id, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        SetupBoards(_board);
    }

    [Fact]
    public async Task ThePublishedLimits_AreTheDecidedOnes()
    {
        var published = await new GetWorkLimitsQueryHandler().Handle(new GetWorkLimitsQuery(), CancellationToken.None);

        published.Data.Should().Be(new WorkLimitsDto(25_000, 200, 50, 50, 200));
    }

    [Fact]
    public async Task The201stField_IsRefusedNamingTheLimit_AndThe200thIsCreated()
    {
        var fields = Fields(199);
        SetupBoardFields([.. fields]);
        var handler = new CreateBoardFieldCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, Placement);

        (await handler.Handle(new CreateBoardFieldCommand(_board.Id, "Field 200", "Text", null), CancellationToken.None)).Succeeded.Should().BeTrue();

        SetupBoardFields([.. Fields(200)]);
        var act = () => handler.Handle(new CreateBoardFieldCommand(_board.Id, "Field 201", "Text", null), CancellationToken.None);

        var refused = (await act.Should().ThrowAsync<AppConflict>()).Which;
        refused.ErrorCode.Should().Be(WorkLimits.FieldsCode);
        refused.Extensions.Should().Contain(new KeyValuePair<string, object?>("limit", "fields"))
            .And.Contain(new KeyValuePair<string, object?>("current", 200))
            .And.Contain(new KeyValuePair<string, object?>("max", 200));
    }

    [Fact]
    public async Task ADeletedFieldDoesNotCount()
    {
        var fields = Fields(200);
        fields[0].Delete(TestUserId, TestNow);
        SetupBoardFields([.. fields]);
        var handler = new CreateBoardFieldCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, Placement);

        (await handler.Handle(new CreateBoardFieldCommand(_board.Id, "Field 201", "Text", null), CancellationToken.None)).Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task The51stGroup_IsRefusedNamingTheLimit()
    {
        SetupBoardGroups([.. Enumerable.Range(0, 50).Select(i => BoardGroup.Create(
            TestAccountId, TestWorkspaceId, _board.Id, $"Group {i}", Color.Create("#808080"), Keys(50)[i], TestUserId, TestNow))]);
        var handler = new CreateBoardGroupCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, Placement);

        var act = () => handler.Handle(new CreateBoardGroupCommand(_board.Id, "Group 51", "#FF0000"), CancellationToken.None);

        var refused = (await act.Should().ThrowAsync<AppConflict>()).Which;
        refused.ErrorCode.Should().Be(WorkLimits.GroupsCode);
        refused.Extensions["max"].Should().Be(50);
    }

    [Fact]
    public async Task The51stView_IsRefusedNamingTheLimit()
    {
        SetupBoardViews([.. Enumerable.Range(0, 50).Select(i => BoardView.Create(
            TestAccountId, TestWorkspaceId, _board.Id, $"View {i}", ViewType.Table, BoardViewConfig.Create(JsonValue.Create("{}")), TestUserId, TestNow))]);
        var handler = new CreateBoardViewCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

        var act = () => handler.Handle(new CreateBoardViewCommand(_board.Id, "View 51", "Table", "{}"), CancellationToken.None);

        (await act.Should().ThrowAsync<AppConflict>()).Which.ErrorCode.Should().Be(WorkLimits.ViewsCode);
    }

    [Fact]
    public async Task AnItemOverTheActiveItemLimit_IsRefused_AndArchivedItemsDoNotCount()
    {
        SetupBoardGroups(_group);
        var items = Enumerable.Range(0, WorkLimits.ActiveItemsPerBoard).Select(_ => CreateBoardItem(boardId: _board.Id, groupId: _group.Id)).ToList();
        DbContextMock.Setup(c => c.BoardItems).Returns(CreateAsyncDbSet(items));
        var handler = new CreateBoardItemCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, Placement, KeyAllocator);

        var act = () => handler.Handle(new CreateBoardItemCommand(_board.Id, _group.Id, "One too many"), CancellationToken.None);

        var refused = (await act.Should().ThrowAsync<AppConflict>()).Which;
        refused.ErrorCode.Should().Be(WorkLimits.ItemsCode);
        refused.Extensions["current"].Should().Be(25_000);
        items.Should().HaveCount(WorkLimits.ActiveItemsPerBoard, "a refused create adds nothing");

        items[0].Archive(TestUserId, TestNow);
        (await handler.Handle(new CreateBoardItemCommand(_board.Id, _group.Id, "Fits"), CancellationToken.None)).Name.Should().Be("Fits");
    }

    [Fact]
    public async Task UnarchivingAnItemOnAFullBoard_IsRefused()
    {
        var items = Enumerable.Range(0, WorkLimits.ActiveItemsPerBoard).Select(_ => CreateBoardItem(boardId: _board.Id, groupId: _group.Id)).ToList();
        var archived = CreateBoardItem(boardId: _board.Id, groupId: _group.Id);
        archived.Archive(TestUserId, TestNow);
        items.Add(archived);
        DbContextMock.Setup(c => c.BoardItems).Returns(CreateAsyncDbSet(items));
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.UserId).Returns(TestUserId);
        var handler = new UnarchiveBoardItemCommandHandler(DbContextMock.Object, currentUser.Object, DateTimeProviderMock.Object);

        var act = () => handler.Handle(new UnarchiveBoardItemCommand(archived.Id, archived.Version), CancellationToken.None);

        (await act.Should().ThrowAsync<AppConflict>()).Which.ErrorCode.Should().Be(WorkLimits.ItemsCode);
        archived.IsArchived.Should().BeTrue();
    }

    private static IReadOnlyList<FractionalIndex> Keys(int count) => FractionalIndexGenerator.GenerateNKeysBetween(null, null, count);

    private List<BoardField> Fields(int count) =>
        Enumerable.Range(0, count).Select(i => BoardField.Create(
            TestAccountId, TestWorkspaceId, _board.Id, $"Field {i}", FieldType.Text, FieldSettings.Empty(),
            Keys(count)[i], TestUserId, TestNow)).ToList();
}
