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

        var command = new UpdateBoardItemCommand(item.Id, "Updated Title", null, null, null, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SetTimeline_Succeeds()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);
        var dueDate = new DateTime(2025, 3, 1);

        var command = new UpdateBoardItemCommand(item.Id, null, null, null, null, dueDate, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ItemNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateBoardItemCommand(Guid.CreateVersion7(), "Title", null, null, null, null, null);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DeletedItem_ThrowsNotFoundException()
    {
        var item = CreateBoardItem();
        item.SoftDelete(TestUserId, TestNow);
        SetupBoardItems(item);

        var command = new UpdateBoardItemCommand(item.Id, "Title", null, null, null, null, null);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_NullFields_NoChanges()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);

        var command = new UpdateBoardItemCommand(item.Id, null, null, null, null, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }
}
