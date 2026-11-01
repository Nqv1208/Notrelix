using Notrelix.Application.Features.Workspaces.Spaces.Commands.ArchiveSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Services;

namespace Notrelix.Application.Tests.Features.Workspaces.Spaces.Commands;

public class ArchiveSpaceCommandHandlerTests : WorkspaceHandlerTestBase
{
    private readonly Mock<ISpaceContentReader> _content = new();

    public ArchiveSpaceCommandHandlerTests()
    {
        _content.Setup(c => c.ReadAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SpaceContentFact.Empty);
    }

    private ArchiveSpaceCommandHandler CreateSut() => new(
        DbContextMock.Object, _content.Object, RequestContextMock.Object, DateTimeProviderMock.Object,
        new Mock<Notrelix.Application.Common.Realtime.IAccessChangeSignal>().Object);

    [Fact]
    public async Task Handle_WhenSpaceExists_ArchivesSuccessfully()
    {
        var space = CreateSpace();
        SetupSpaces(space);
        var sut = CreateSut();
        var result = await sut.Handle(new ArchiveSpaceCommand(TestWorkspaceId, space.Id, space.Version), CancellationToken.None);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenSpaceNotFound_ThrowsNotFoundException()
    {
        SetupSpaces();
        var sut = CreateSut();
        Func<Task> act = () => sut.Handle(new ArchiveSpaceCommand(TestWorkspaceId, Guid.CreateVersion7(), 1), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
