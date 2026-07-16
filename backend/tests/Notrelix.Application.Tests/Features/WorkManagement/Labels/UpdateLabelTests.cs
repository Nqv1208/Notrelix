using Notrelix.Application.Features.WorkManagement.Labels.Commands.UpdateLabel;

namespace Notrelix.Application.Tests.Features.WorkManagement.Labels;

public class UpdateLabelTests : WorkManagementHandlerTestBase
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly UpdateLabelCommandHandler _handler;

    public UpdateLabelTests()
    {
        _currentUserMock.Setup(c => c.UserId).Returns(TestUserId);
        _handler = new UpdateLabelCommandHandler(
            DbContextMock.Object,
            _currentUserMock.Object,
            DateTimeProviderMock.Object);
    }

    [Fact]
    public async Task Handle_UpdateNameAndColor_UpdatesLabel()
    {
        var label = Label.Create(
            TestAccountId, TestWorkspaceId, Guid.CreateVersion7(),
            "Bug", LabelColor.Create("#FF0000"), TestUserId, TestNow);
        SetupLabels(label);

        var command = new UpdateLabelCommand(label.Id, "Feature", "#00FF00");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UpdateColorOnly_UpdatesLabel()
    {
        var label = Label.Create(
            TestAccountId, TestWorkspaceId, Guid.CreateVersion7(),
            "Bug", LabelColor.Create("#FF0000"), TestUserId, TestNow);
        SetupLabels(label);

        var command = new UpdateLabelCommand(label.Id, null, "#0000FF");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UpdateNameOnly_UpdatesLabel()
    {
        var label = Label.Create(
            TestAccountId, TestWorkspaceId, Guid.CreateVersion7(),
            "Bug", LabelColor.Create("#FF0000"), TestUserId, TestNow);
        SetupLabels(label);

        var command = new UpdateLabelCommand(label.Id, "Enhancement", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_LabelNotFound_ThrowsNotFoundException()
    {
        var command = new UpdateLabelCommand(Guid.CreateVersion7(), "Name", "#FF0000");

        await _handler.Invoking(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
