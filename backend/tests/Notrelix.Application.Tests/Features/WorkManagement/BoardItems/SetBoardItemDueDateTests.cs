using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.SetBoardItemDueDate;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class SetBoardItemDueDateTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly SetBoardItemDueDateCommandHandler _handler;

    public SetBoardItemDueDateTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);
        _handler = new SetBoardItemDueDateCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_SetDueDate_Succeeds()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);
        var dueDate = new DateTime(2025, 2, 1);

        var command = new SetBoardItemDueDateCommand(item.Id, dueDate, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SetBothDates_Succeeds()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);
        var startDate = new DateTime(2025, 1, 1);
        var dueDate = new DateTime(2025, 2, 1);

        var command = new SetBoardItemDueDateCommand(item.Id, dueDate, startDate);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ClearDates_Succeeds()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);

        var command = new SetBoardItemDueDateCommand(item.Id, null, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ItemNotFound_ThrowsNotFoundException()
    {
        var command = new SetBoardItemDueDateCommand(Guid.CreateVersion7(), new DateTime(2025, 2, 1), null);

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
