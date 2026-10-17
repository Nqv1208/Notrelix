using Notrelix.Application.Features.Workspaces.Spaces.Commands.CreateSpace;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.Workspaces.Spaces.Commands;

public class CreateSpaceCommandHandlerTests : WorkspaceHandlerTestBase
{
    private readonly InMemorySpaceStore _spaces = new();

    private CreateSpaceCommandHandler CreateSut() => new(
        DbContextMock.Object, _spaces, RequestContextMock.Object, DateTimeProviderMock.Object);

    [Fact]
    public async Task Handle_WhenWorkspaceExists_CreatesSpaceSuccessfully()
    {
        var workspace = CreateWorkspace();
        SetupWorkspaces(workspace);
        SetupSpaces();
        var sut = CreateSut();
        var command = new CreateSpaceCommand(workspace.Id, "New Space", "Workspace", "A description");
        var result = await sut.Handle(command, CancellationToken.None);
        result.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WhenWorkspaceNotFound_ThrowsNotFoundException()
    {
        SetupWorkspaces();
        var sut = CreateSut();
        var command = new CreateSpaceCommand(TestWorkspaceId, "New Space", "Workspace", null);
        Func<Task> act = () => sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
