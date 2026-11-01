using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveBoardItemToBoard;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.ListBoardItemTrash;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class MoveBoardItemToBoardTests : WorkManagementHandlerTestBase
{
    private readonly Mock<IBoardItemTargetAuthorizer> _target = new();
    private readonly Mock<IResourceVisibility> _visibility = new();
    private readonly MoveBoardItemToBoardCommandHandler _handler;
    private readonly Board _source;
    private readonly Board _destination;
    private readonly BoardGroup _sourceGroup;
    private readonly BoardGroup _destinationGroup;
    private readonly BoardField _sourceStatus;
    private readonly BoardField _destinationStatus;
    private readonly BoardItem _item;

    public MoveBoardItemToBoardTests()
    {
        DbContextMock.Setup(c => c.ItemDependencies).Returns(CreateAsyncDbSet(new List<ItemDependency>()));
        DbContextMock.Setup(c => c.TimeTrackingEntries).Returns(CreateAsyncDbSet(new List<TimeTrackingEntry>()));
        _handler = new MoveBoardItemToBoardCommandHandler(
            DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, Placement, KeyAllocator, _target.Object, _visibility.Object);

        _source = CreateBoard();
        _destination = CreateBoard();
        _sourceGroup = BoardGroup.Create(TestAccountId, TestWorkspaceId, _source.Id, "Todo", Color.Create("#111111"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        _destinationGroup = BoardGroup.Create(TestAccountId, TestWorkspaceId, _destination.Id, "Backlog", Color.Create("#222222"), FractionalIndex.Create("a0"), TestUserId, TestNow);
        _sourceStatus = StatusField(_source.Id, "Open", "Done");
        _destinationStatus = StatusField(_destination.Id, "New", "Done");
        _item = CreateBoardItem(boardId: _source.Id, groupId: _sourceGroup.Id);
        var done = _sourceStatus.Options.First(o => o.Name == "Done");
        _item.UpdateFieldValue(_sourceStatus, FieldValue.Create(JsonValue.Create($"\"{done.Id}\"")!), TestUserId, TestNow);

        SetupBoards(_source, _destination);
        SetupBoardGroups(_sourceGroup, _destinationGroup);
        SetupBoardFields(_sourceStatus, _destinationStatus);
        SetupBoardItems(_item);
        // The handler reads the held values from the value set before it deletes them.
        DbContextMock.Setup(c => c.BoardItemValues).Returns(CreateAsyncDbSet(_item.FieldValues.ToList()));
    }

    private BoardField StatusField(Guid boardId, params string[] options)
    {
        var field = BoardField.Create(
            TestAccountId, TestWorkspaceId, boardId, "Status",
            FieldType.Status, FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")!), FractionalIndex.Create("a0"),
            TestUserId, TestNow);
        for (var i = 0; i < options.Length; i++)
            field.AddOption(options[i], Color.Create("#00FF00"), FractionalIndex.Create($"a{i}"), TestUserId, TestNow, i == 1 ? WorkStatusCategory.Done : WorkStatusCategory.Todo);
        return field;
    }

    [Fact]
    public async Task Handle_MovesTheItemAndMapsItsStatusByName()
    {
        var result = await _handler.Handle(new MoveBoardItemToBoardCommand(_item.Id, _destination.Id, ExpectedVersion: 1), CancellationToken.None);

        _target.Verify(t => t.AuthorizeCreateAsync(_destination.Id, It.IsAny<CancellationToken>()), Times.Once);
        _item.BoardId.Should().Be(_destination.Id);
        _item.GroupId.Should().Be(_destinationGroup.Id);
        result.BoardId.Should().Be(_destination.Id);
        result.ItemKey.Should().Be(_item.ItemKey);
        var status = _item.FieldValues.Single(v => v.FieldId == _destinationStatus.Id);
        var doneId = _destinationStatus.Options.First(o => o.Name == "Done").Id;
        status.Value.Data.Value.Should().Contain(doneId.ToString());
    }

    [Fact]
    public async Task Handle_AStatusWithoutANamesake_LandsInTheTargetsFirstStatus()
    {
        var other = StatusField(_destination.Id, "New", "Review");
        SetupBoardFields(_sourceStatus, other);

        await _handler.Handle(new MoveBoardItemToBoardCommand(_item.Id, _destination.Id, ExpectedVersion: 1), CancellationToken.None);

        var first = other.Options.OrderBy(o => o.Position.Value, StringComparer.Ordinal).First().Id;
        _item.FieldValues.Single(v => v.FieldId == other.Id).Value.Data.Value.Should().Contain(first.ToString());
    }

    [Fact]
    public async Task Handle_AssigneeWhoCannotViewTheTarget_IsDroppedAndCounted()
    {
        var stays = Guid.CreateVersion7();
        var leaves = Guid.CreateVersion7();
        SetupBoardItemMembers(
            BoardItemMember.Create(TestAccountId, TestWorkspaceId, _source.Id, _item.Id, stays, null, TestNow),
            BoardItemMember.Create(TestAccountId, TestWorkspaceId, _source.Id, _item.Id, leaves, null, TestNow));
        _visibility.Setup(v => v.VisibleBoardIdsAsync(TestWorkspaceId, stays, It.IsAny<CancellationToken>())).ReturnsAsync([_destination.Id]);
        _visibility.Setup(v => v.VisibleBoardIdsAsync(TestWorkspaceId, leaves, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await _handler.Handle(new MoveBoardItemToBoardCommand(_item.Id, _destination.Id, ExpectedVersion: 1), CancellationToken.None);

        result.DroppedAssignees.Should().Be(1);
        DbContextMock.Object.BoardItemMembers.Single(m => m.UserId == stays).BoardId.Should().Be(_destination.Id);
    }

    [Fact]
    public async Task Handle_TheSameBoard_IsRefused()
    {
        await _handler.Invoking(h => h.Handle(new MoveBoardItemToBoardCommand(_item.Id, _source.Id, ExpectedVersion: 1), CancellationToken.None))
            .Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
    }

    [Fact]
    public async Task Handle_WhenTheCallerMayNotCreateOnTheTarget_NothingMoves()
    {
        _target.Setup(t => t.AuthorizeCreateAsync(_destination.Id, It.IsAny<CancellationToken>())).ThrowsAsync(new ForbiddenException("no"));

        await _handler.Invoking(h => h.Handle(new MoveBoardItemToBoardCommand(_item.Id, _destination.Id, ExpectedVersion: 1), CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();
        _item.BoardId.Should().Be(_source.Id);
    }

    [Fact]
    public async Task Handle_AnArchivedItem_IsRefused()
    {
        _item.Archive(TestUserId, TestNow);

        await _handler.Invoking(h => h.Handle(new MoveBoardItemToBoardCommand(_item.Id, _destination.Id, ExpectedVersion: 1), CancellationToken.None))
            .Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
    }

    [Fact]
    public async Task Trash_ListsOnlyRecentDeletionsOfTheBoard()
    {
        var recent = CreateBoardItem(boardId: _source.Id);
        DateTimeProviderMock.Setup(c => c.UtcNow).Returns(TestNow.AddDays(40));
        recent.Delete(TestUserId, TestNow.AddDays(38));
        var stale = CreateBoardItem(boardId: _source.Id);
        stale.Delete(TestUserId, TestNow.AddDays(5));
        var elsewhere = CreateBoardItem(boardId: _destination.Id);
        elsewhere.Delete(TestUserId, TestNow.AddDays(39));
        SetupBoardItems(_item, recent, stale, elsewhere);
        var query = new ListBoardItemTrashQueryHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

        var rows = await query.Handle(new ListBoardItemTrashQuery(_source.Id), CancellationToken.None);

        rows.Should().ContainSingle().Which.Id.Should().Be(recent.Id);
        rows[0].RestorableUntil.Should().Be(TestNow.AddDays(38).AddDays(30));
    }
}
