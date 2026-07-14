using Notrelix.Application.Features.Workspaces.Settings.Commands.UpdateWorkspaceSettings;

namespace Notrelix.Application.Tests.Features.Workspaces.Settings.Commands;

public class UpdateWorkspaceSettingsCommandHandlerTests : WorkspaceHandlerTestBase
{
    private UpdateWorkspaceSettingsCommandHandler CreateSut() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

    [Fact]
    public async Task Handle_WhenWorkspaceExists_UpdatesSettings()
    {
        var workspace = CreateWorkspace();
        SetupWorkspaces(workspace);
        var sut = CreateSut();
        var command = new UpdateWorkspaceSettingsCommand(workspace.Id, true, false, true, "Member", 7, 1);
        var result = await sut.Handle(command, CancellationToken.None);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenWorkspaceNotFound_ThrowsNotFoundException()
    {
        SetupWorkspaces();
        var sut = CreateSut();
        var command = new UpdateWorkspaceSettingsCommand(TestWorkspaceId, true, false, true, "Member", 7, 1);
        Func<Task> act = () => sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
