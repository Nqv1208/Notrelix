using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItem;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class UpdateBoardItemTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly UpdateBoardItemCommandHandler _handler;

    public UpdateBoardItemTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);
        _handler = new UpdateBoardItemCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_RenameItem_Succeeds()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);

        var command = new UpdateBoardItemCommand(item.Id, "Updated Title", null, null, 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SetTimeline_Succeeds()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);
        var dueDate = new DateTime(2025, 3, 1);

        var command = new UpdateBoardItemCommand(item.Id, null, dueDate, null, 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ItemNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateBoardItemCommand(Guid.CreateVersion7(), "Title", null, null, 1);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DeletedItem_ThrowsNotFoundException()
    {
        var item = CreateBoardItem();
        item.Delete(TestUserId, TestNow);
        SetupBoardItems(item);

        var command = new UpdateBoardItemCommand(item.Id, "Title", null, null, 1);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_NullFields_NoChanges()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);

        var command = new UpdateBoardItemCommand(item.Id, null, null, null, 1);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SetDescription_StoresItAndAdvancesTheVersion()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);
        var version = item.Version;

        var result = await _handler.Handle(new UpdateBoardItemCommand(item.Id, null, null, null, 1, "What and why"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        item.DescriptionMd.Should().Be("What and why");
        item.Version.Should().Be(version + 1);
    }

    [Fact]
    public async Task Handle_EmptyDescription_ClearsIt_AndOmittedLeavesItAlone()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);
        await _handler.Handle(new UpdateBoardItemCommand(item.Id, null, null, null, 1, "Keep me"), CancellationToken.None);

        await _handler.Handle(new UpdateBoardItemCommand(item.Id, "Renamed", null, null, 1, null), CancellationToken.None);
        item.DescriptionMd.Should().Be("Keep me");

        await _handler.Handle(new UpdateBoardItemCommand(item.Id, null, null, null, 1, ""), CancellationToken.None);
        item.DescriptionMd.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ClearDueDate_RemovesTheDueDate_AndKeepsTheStartDate()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);
        await _handler.Handle(new UpdateBoardItemCommand(item.Id, null, new DateTime(2026, 11, 9), new DateTime(2026, 11, 2), 1), CancellationToken.None);

        await _handler.Handle(new UpdateBoardItemCommand(item.Id, null, null, null, 1, ClearDueDate: true), CancellationToken.None);

        item.DueAt.Should().BeNull();
        item.StartedAt.Should().Be(new DateTimeOffset(new DateTime(2026, 11, 2), TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_NullDates_WithoutAClearFlag_LeaveTheDatesAlone()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);
        await _handler.Handle(new UpdateBoardItemCommand(item.Id, null, new DateTime(2026, 11, 9), null, 1), CancellationToken.None);

        await _handler.Handle(new UpdateBoardItemCommand(item.Id, "Renamed", null, null, 1), CancellationToken.None);

        item.DueAt.Should().NotBeNull();
    }

    [Theory]
    [InlineData(500, true)]
    [InlineData(501, false)]
    public void Validator_TakesATitleUpToWhatTheDomainTakes(int length, bool valid)
    {
        BoardItem.NameMaxLength.Should().Be(500, "create and rename share the Domain limit");

        new UpdateBoardItemCommandValidator()
            .Validate(new UpdateBoardItemCommand(Guid.CreateVersion7(), new string('x', length), null, null, 1))
            .IsValid.Should().Be(valid);
    }

    /// <summary>
    /// UC-WM-L01, D76.9: a dependency line never reschedules. Moving the item another item waits on past that
    /// item's start writes the moved item only; the waiting item keeps its dates and its version.
    /// </summary>
    [Fact]
    public async Task Handle_MovingAnItemOthersWaitOn_NeverReschedulesTheWaitingItem()
    {
        var before = CreateBoardItem();
        var waiting = CreateBoardItem(boardId: before.BoardId);
        var day = new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero);
        before.SetTimeline(day, day.AddDays(2), TestUserId, TestNow);
        waiting.SetTimeline(day.AddDays(3), day.AddDays(5), TestUserId, TestNow);
        var waitingVersion = waiting.Version;
        var dependency = BoardItemLink.Create(
            TestAccountId, TestWorkspaceId, Guid.CreateVersion7(), waiting.Id,
            ResourceRef.Create(ResourceKind.Create("work-management.board-item"), before.Id, TestWorkspaceId),
            BoardItemLinkType.Dependency, TestUserId, TestNow);
        SetupBoardItems(before, waiting);
        SetupBoardItemLinks(dependency);

        var result = await _handler.Handle(
            new UpdateBoardItemCommand(before.Id, null, day.AddDays(8).UtcDateTime, day.AddDays(6).UtcDateTime, before.Version),
            CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        before.StartedAt.Should().Be(day.AddDays(6));
        before.DueAt.Should().Be(day.AddDays(8));
        waiting.StartedAt.Should().Be(day.AddDays(3));
        waiting.DueAt.Should().Be(day.AddDays(5));
        waiting.Version.Should().Be(waitingVersion);
    }
}
