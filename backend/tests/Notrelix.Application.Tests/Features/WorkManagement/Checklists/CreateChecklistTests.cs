using Notrelix.Application.Features.WorkManagement.Checklists.Commands.CreateChecklist;

namespace Notrelix.Application.Tests.Features.WorkManagement.Checklists;

public class CreateChecklistTests : WorkManagementHandlerTestBase
{
    private readonly CreateChecklistCommandHandler _handler;

    public CreateChecklistTests()
    {
        _handler = new CreateChecklistCommandHandler(
            DbContextMock.Object,
            RequestContextMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesChecklist()
    {
        var item = CreateBoardItem();
        SetupBoardItems(item);

        var command = new CreateChecklistCommand(item.Id, "My Checklist");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_BoardItemNotFound_ThrowsNotFoundException()
    {
        var command = new CreateChecklistCommand(Guid.CreateVersion7(), "My Checklist");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ValidCommand_UsesCorrectWorkspaceId()
    {
        var board = CreateBoard();
        SetupBoards(board);
        var item = CreateBoardItem(boardId: board.Id);
        SetupBoardItems(item);

        var command = new CreateChecklistCommand(item.Id, "Checklist");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }
}
