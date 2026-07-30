using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ArchiveBoardItem;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class ArchiveBoardItemTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly ArchiveBoardItemCommandHandler _handler;

    public ArchiveBoardItemTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);
        _handler = new ArchiveBoardItemCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_DeletesItem()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);

        var command = new ArchiveBoardItemCommand(item.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ItemNotFound_ThrowsNotFoundException()
    {
        var command = new ArchiveBoardItemCommand(Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
