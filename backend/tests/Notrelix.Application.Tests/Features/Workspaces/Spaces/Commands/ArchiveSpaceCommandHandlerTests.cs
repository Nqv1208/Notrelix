using Notrelix.Application.Features.Workspaces.Spaces.Commands.ArchiveSpace;

namespace Notrelix.Application.Tests.Features.Workspaces.Spaces.Commands;

public class ArchiveSpaceCommandHandlerTests : WorkspaceHandlerTestBase
{
    private ArchiveSpaceCommandHandler CreateSut() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

    [Fact]
    public async Task Handle_WhenSpaceExists_ArchivesSuccessfully()
    {
        var space = CreateSpace();
        SetupSpaces(space);
        var sut = CreateSut();
        var result = await sut.Handle(new ArchiveSpaceCommand(TestWorkspaceId, space.Id), CancellationToken.None);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenSpaceNotFound_ThrowsNotFoundException()
    {
        SetupSpaces();
        var sut = CreateSut();
        Func<Task> act = () => sut.Handle(new ArchiveSpaceCommand(TestWorkspaceId, Guid.CreateVersion7()), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
