using Notrelix.Application.Features.WorkManagement.Labels.Commands.DeleteLabel;

namespace Notrelix.Application.Tests.Features.WorkManagement.Labels;

public class DeleteLabelTests : WorkManagementHandlerTestBase
{
    private readonly DeleteLabelCommandHandler _handler;

    public DeleteLabelTests()
    {
        _handler = new DeleteLabelCommandHandler(DbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_DeletesLabel()
    {
        var label = Label.Create(
            TestAccountId, TestWorkspaceId, Guid.CreateVersion7(),
            "Bug", LabelColor.Create("#FF0000"), TestUserId, TestNow);
        SetupLabels(label);

        var command = new DeleteLabelCommand(label.Id);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_LabelNotFound_ThrowsNotFoundException()
    {
        var command = new DeleteLabelCommand(Guid.CreateVersion7());

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
