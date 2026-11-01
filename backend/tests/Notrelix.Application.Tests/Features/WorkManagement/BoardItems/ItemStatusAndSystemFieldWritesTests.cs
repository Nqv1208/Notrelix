using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ClearFieldValue;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValue;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValues;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemStatus;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

/// <summary>
/// Status writes resolve the Board's Status field like the read path (first Status
/// field by position, then id) and keep completion in step with the done category,
/// on every write path. The system Due Date field writes the Item's DueAt, never a
/// parallel value; the system Title field takes no raw value.
/// </summary>
public class ItemStatusAndSystemFieldWritesTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Board _board;
    private readonly BoardItem _item;
    private readonly BoardField _status;
    private readonly BoardField _dueDate;
    private readonly BoardField _title;

    public ItemStatusAndSystemFieldWritesTests()
    {
        _currentUser.Setup(u => u.UserId).Returns(TestUserId);
        _board = CreateBoard();
        _item = CreateBoardItem(boardId: _board.Id);
        _status = SystemField("Status", FieldType.Status, "a1");
        _status.AddDefaultStatusOptions(TestUserId, TestNow);
        _dueDate = SystemField("Due Date", FieldType.Date, "a3");
        _title = SystemField("Title", FieldType.Text, "a0");
        SetupBoards(_board);
        SetupBoardItems(_item);
    }

    private BoardField SystemField(string name, FieldType type, string position) =>
        BoardField.Create(TestAccountId, TestWorkspaceId, _board.Id, name, type,
            type == FieldType.Status ? FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")) : FieldSettings.Empty(),
            FractionalIndex.Create(position), TestUserId, TestNow, isSystem: true);

    private Guid Option(BoardField field, WorkStatusCategory category) => field.Options.Single(o => o.Category == category).Id;

    private UpdateBoardItemStatusCommandHandler StatusHandler() => new(DbContextMock.Object, _currentUser.Object, DateTimeProviderMock.Object);

    [Fact]
    public async Task Status_ResolvesTheFirstStatusFieldByPosition_NotTheFirstLoaded()
    {
        var later = BoardField.Create(TestAccountId, TestWorkspaceId, _board.Id, "Stage", FieldType.Status,
            FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")), FractionalIndex.Create("a5"), TestUserId, TestNow);
        later.AddDefaultStatusOptions(TestUserId, TestNow);
        SetupBoardFields(later, _status);

        var result = await StatusHandler().Handle(new UpdateBoardItemStatusCommand(_item.Id, Option(_status, WorkStatusCategory.Review).ToString(), ExpectedVersion: 1), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _item.FieldValues.Should().ContainSingle().Which.FieldId.Should().Be(_status.Id);
    }

    [Fact]
    public async Task Status_IntoDone_CompletesTheItem_AndOutOfDone_ReopensIt()
    {
        SetupBoardFields(_status, _dueDate, _title);

        await StatusHandler().Handle(new UpdateBoardItemStatusCommand(_item.Id, Option(_status, WorkStatusCategory.Done).ToString(), ExpectedVersion: 1), CancellationToken.None);
        _item.CompletedAt.Should().Be(TestNow);

        await StatusHandler().Handle(new UpdateBoardItemStatusCommand(_item.Id, Option(_status, WorkStatusCategory.InProgress).ToString(), ExpectedVersion: 1), CancellationToken.None);
        _item.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task FieldValue_OnTheStatusField_AlsoKeepsCompletionInStep()
    {
        SetupBoardFields(_status, _dueDate, _title);
        var handler = new UpdateBoardItemFieldValueCommandHandler(DbContextMock.Object, _currentUser.Object, DateTimeProviderMock.Object);

        await handler.Handle(new UpdateBoardItemFieldValueCommand(_item.Id, _status.Id, Option(_status, WorkStatusCategory.Done).ToString(), 1), CancellationToken.None);

        _item.CompletedAt.Should().Be(TestNow);
    }

    [Fact]
    public async Task BulkValues_OnTheStatusField_AlsoKeepCompletionInStep()
    {
        SetupBoardFields(_status, _dueDate, _title);
        var handler = new UpdateBoardItemFieldValuesCommandHandler(DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, Mock.Of<IResourceLocator>());

        await handler.Handle(new UpdateBoardItemFieldValuesCommand(_item.Id,
            new Dictionary<Guid, object?> { [_status.Id] = Option(_status, WorkStatusCategory.Done).ToString() }, 1), CancellationToken.None);

        _item.CompletedAt.Should().Be(TestNow);
    }

    [Fact]
    public async Task ClearingTheStatus_ReopensTheItem()
    {
        SetupBoardFields(_status, _dueDate, _title);
        await StatusHandler().Handle(new UpdateBoardItemStatusCommand(_item.Id, Option(_status, WorkStatusCategory.Done).ToString(), ExpectedVersion: 1), CancellationToken.None);

        await new ClearFieldValueCommandHandler(DbContextMock.Object, _currentUser.Object, DateTimeProviderMock.Object)
            .Handle(new ClearFieldValueCommand(_item.Id, _status.Id, 1), CancellationToken.None);

        _item.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task FieldValue_OnTheSystemDueDate_WritesDueAt_AndStoresNoValue()
    {
        SetupBoardFields(_status, _dueDate, _title);
        var due = new DateTimeOffset(2026, 10, 18, 0, 0, 0, TimeSpan.FromHours(7));
        var handler = new UpdateBoardItemFieldValueCommandHandler(DbContextMock.Object, _currentUser.Object, DateTimeProviderMock.Object);

        await handler.Handle(new UpdateBoardItemFieldValueCommand(_item.Id, _dueDate.Id, due.ToString("O"), 1), CancellationToken.None);

        _item.DueAt.Should().Be(due);
        _item.FieldValues.Should().BeEmpty("BoardItem.DueAt is the authoritative due date");

        await new ClearFieldValueCommandHandler(DbContextMock.Object, _currentUser.Object, DateTimeProviderMock.Object)
            .Handle(new ClearFieldValueCommand(_item.Id, _dueDate.Id, 1), CancellationToken.None);

        _item.DueAt.Should().BeNull();
        _item.FieldValues.Should().BeEmpty();
    }

    [Fact]
    public async Task FieldValue_OnTheSystemTitle_IsRejected()
    {
        SetupBoardFields(_status, _dueDate, _title);
        var handler = new UpdateBoardItemFieldValueCommandHandler(DbContextMock.Object, _currentUser.Object, DateTimeProviderMock.Object);

        var act = () => handler.Handle(new UpdateBoardItemFieldValueCommand(_item.Id, _title.Id, "Renamed", 1), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Field_CannotUpdateSystem);
        _item.Name.Should().Be("Test Item");
        _item.FieldValues.Should().BeEmpty();
    }
}
