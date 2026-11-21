using Notrelix.Application.Features.Workspaces.Spaces.Commands.DeleteSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Commands.RestoreSpace;
using Notrelix.Application.Features.Workspaces.Spaces.Services;
using Notrelix.Domain.Governance.Permissions;

namespace Notrelix.Application.Tests.Features.Workspaces.Spaces.Commands;

public class DeleteSpaceCommandHandlerTests : WorkspaceHandlerTestBase
{
    private readonly Mock<ISpaceContentReader> _content = new();

    public DeleteSpaceCommandHandlerTests()
    {
        _content.Setup(c => c.ReadAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SpaceContentFact.Empty);
    }

    private DeleteSpaceCommandHandler CreateSut() => new(
        DbContextMock.Object, _content.Object, RequestContextMock.Object, DateTimeProviderMock.Object,
        new Mock<Notrelix.Application.Common.Realtime.IAccessChangeSignal>().Object);

    [Fact]
    public async Task Handle_WhenSpaceExists_DeletesIt()
    {
        var space = CreateSpace();
        SetupSpaces(space);

        var result = await CreateSut().Handle(new DeleteSpaceCommand(TestWorkspaceId, space.Id, space.Version), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        space.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenAlreadyDeleted_IsANoOp_WithoutANewVersion()
    {
        var space = CreateSpace();
        SetupSpaces(space);
        var sut = CreateSut();
        await sut.Handle(new DeleteSpaceCommand(TestWorkspaceId, space.Id, space.Version), CancellationToken.None);
        var version = space.Version;

        var again = await sut.Handle(new DeleteSpaceCommand(TestWorkspaceId, space.Id, version), CancellationToken.None);

        again.Succeeded.Should().BeTrue();
        space.Version.Should().Be(version);
    }

    [Fact]
    public async Task Handle_WhenSpaceNotFound_ThrowsNotFoundException()
    {
        SetupSpaces();

        Func<Task> act = () => CreateSut().Handle(new DeleteSpaceCommand(TestWorkspaceId, Guid.CreateVersion7(), 1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void Delete_BindsItsExpectedVersionToTheSpace_AndRestoreStaysVersionless()
    {
        var delete = new DeleteSpaceCommand(TestWorkspaceId, Guid.CreateVersion7(), 3);

        ((IExpectedVersionRequest)delete).ExpectedVersion.Should().Be(3);
        delete.Resource.Kind.Value.Should().Be("workspaces.space");
        delete.Action.Should().Be(PermissionAction.ManageSpaces);
        new DeleteSpaceCommandValidator().Validate(delete with { ExpectedVersion = 0 }).IsValid.Should().BeFalse();

        var restore = new RestoreSpaceCommand(TestWorkspaceId, delete.SpaceId);
        restore.Should().NotBeAssignableTo<IExpectedVersionRequest>();
        restore.Action.Should().Be(PermissionAction.ManageSpaces);
    }
}
