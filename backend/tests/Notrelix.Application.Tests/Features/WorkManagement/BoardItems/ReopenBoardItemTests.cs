using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ReopenBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemStatus;
using Notrelix.Domain.SharedKernel.Ordering;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

/// <summary>
/// Reopening returns a completed Item to the status it held before completion when
/// that is recorded and still open, otherwise to the board's first open status, and
/// clears who completed it.
/// </summary>
public class ReopenBoardItemTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Board _board;
    private readonly BoardItem _item;
    private readonly BoardField _status;

    public ReopenBoardItemTests()
    {
        _currentUser.Setup(u => u.UserId).Returns(TestUserId);
        _board = CreateBoard();
        _item = CreateBoardItem(boardId: _board.Id);
        _status = BoardField.Create(TestAccountId, TestWorkspaceId, _board.Id, "Status", FieldType.Status,
            FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")), FractionalIndex.Create("a1"), TestUserId, TestNow, isSystem: true);
        _status.AddDefaultStatusOptions(TestUserId, TestNow);
        SetupBoards(_board);
        SetupBoardItems(_item);
        SetupBoardFields(_status);
    }

    private Guid Option(WorkStatusCategory category) => _status.Options.Single(o => o.Category == category).Id;

    private Task<Result> Status(Guid optionId) =>
        new UpdateBoardItemStatusCommandHandler(DbContextMock.Object, _currentUser.Object, DateTimeProviderMock.Object)
            .Handle(new UpdateBoardItemStatusCommand(_item.Id, optionId.ToString(), ExpectedVersion: 1), CancellationToken.None);

    private Task<Result> Reopen() =>
        new ReopenBoardItemCommandHandler(DbContextMock.Object, _currentUser.Object, DateTimeProviderMock.Object)
            .Handle(new ReopenBoardItemCommand(_item.Id, ExpectedVersion: 1), CancellationToken.None);

    private Guid? CurrentStatus() => _item.FieldValues.SingleOrDefault(v => v.FieldId == _status.Id) is { } v
        ? Guid.Parse(v.Value.Data.Value.Trim('"'))
        : null;

    [Fact]
    public async Task ReturnsToTheStatusHeldBeforeCompletion()
    {
        (await Status(Option(WorkStatusCategory.Review))).Succeeded.Should().BeTrue();
        (await Status(Option(WorkStatusCategory.Done))).Succeeded.Should().BeTrue();
        _item.CompletedBy.Should().Be(TestUserId);

        (await Reopen()).Succeeded.Should().BeTrue();

        CurrentStatus().Should().Be(Option(WorkStatusCategory.Review));
        _item.CompletedAt.Should().BeNull();
        _item.CompletedBy.Should().BeNull();
    }

    [Fact]
    public async Task WithoutARecordedStatus_ReturnsToTheFirstOpenStatus()
    {
        (await Status(Option(WorkStatusCategory.Done))).Succeeded.Should().BeTrue();
        _item.StatusBeforeCompletionOptionId.Should().BeNull();

        (await Reopen()).Succeeded.Should().BeTrue();

        CurrentStatus().Should().Be(Option(WorkStatusCategory.Todo));
        _item.CompletedAt.Should().BeNull();
    }

    [Fact]
    public async Task AnItemThatIsNotCompleted_IsLeftUnchanged()
    {
        (await Status(Option(WorkStatusCategory.InProgress))).Succeeded.Should().BeTrue();
        var version = _item.Version;

        (await Reopen()).Succeeded.Should().BeTrue();

        _item.Version.Should().Be(version);
        CurrentStatus().Should().Be(Option(WorkStatusCategory.InProgress));
    }
}
