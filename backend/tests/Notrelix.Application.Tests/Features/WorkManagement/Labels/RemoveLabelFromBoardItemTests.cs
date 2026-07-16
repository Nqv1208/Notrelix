using Notrelix.Application.Features.WorkManagement.Labels.Commands.RemoveLabelFromBoardItem;

namespace Notrelix.Application.Tests.Features.WorkManagement.Labels;

public class RemoveLabelFromBoardItemTests : WorkManagementHandlerTestBase
{
    private readonly RemoveLabelFromBoardItemCommandHandler _handler;

    public RemoveLabelFromBoardItemTests()
    {
        _handler = new RemoveLabelFromBoardItemCommandHandler(DbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_RemovesLabel()
    {
        var boardId = Guid.CreateVersion7();
        var itemId = Guid.CreateVersion7();
        var labelId = Guid.CreateVersion7();
        var link = BoardItemLabel.Create(
            TestAccountId, TestWorkspaceId, boardId, itemId, labelId, TestUserId, TestNow);
        SetupBoardItemLabels(link);

        var command = new RemoveLabelFromBoardItemCommand(itemId, labelId);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_LabelNotApplied_ReturnsSuccess()
    {
        var command = new RemoveLabelFromBoardItemCommand(Guid.CreateVersion7(), Guid.CreateVersion7());

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }
}
