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
}
