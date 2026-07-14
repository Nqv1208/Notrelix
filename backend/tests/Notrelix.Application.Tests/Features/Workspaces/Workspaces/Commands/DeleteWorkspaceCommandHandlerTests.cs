using Notrelix.Application.Features.Workspaces.Workspaces.Commands.DeleteWorkspace;

namespace Notrelix.Application.Tests.Features.Workspaces.Workspaces.Commands;

public class DeleteWorkspaceCommandHandlerTests : WorkspaceHandlerTestBase
{
    private DeleteWorkspaceCommandHandler CreateSut() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

    [Fact]
    public async Task Handle_WhenWorkspaceExists_DeletesSuccessfully()
    {
        var workspace = CreateWorkspace();
        SetupWorkspaces(workspace);
        var sut = CreateSut();
        var result = await sut.Handle(new DeleteWorkspaceCommand(workspace.Id, 1), CancellationToken.None);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenWorkspaceNotFound_ThrowsNotFoundException()
    {
        SetupWorkspaces();
        var sut = CreateSut();
        Func<Task> act = () => sut.Handle(new DeleteWorkspaceCommand(TestWorkspaceId, 1), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
