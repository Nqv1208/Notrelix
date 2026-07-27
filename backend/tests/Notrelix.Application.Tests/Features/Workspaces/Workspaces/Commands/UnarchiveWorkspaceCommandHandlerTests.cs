using Notrelix.Application.Features.Workspaces.Workspaces.Commands.UnarchiveWorkspace;

namespace Notrelix.Application.Tests.Features.Workspaces.Workspaces.Commands;

public class UnarchiveWorkspaceCommandHandlerTests : WorkspaceHandlerTestBase
{
    private UnarchiveWorkspaceCommandHandler CreateSut() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

    [Fact]
    public async Task Handle_WhenWorkspaceExists_Archived_UnarchivesSuccessfully()
    {
        var workspace = CreateWorkspace(id: TestWorkspaceId, isArchived: true);
        SetupWorkspaces(workspace);
        DateTimeProviderMock.Setup(c => c.UtcNow).Returns(TestNow.AddDays(2));
        var sut = CreateSut();
        var result = await sut.Handle(new UnarchiveWorkspaceCommand(TestWorkspaceId, 1), CancellationToken.None);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenWorkspaceNotFound_ThrowsNotFoundException()
    {
        SetupWorkspaces();
        var sut = CreateSut();
        Func<Task> act = () => sut.Handle(new UnarchiveWorkspaceCommand(TestWorkspaceId, 1), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
