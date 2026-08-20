using Notrelix.Application.Features.Workspaces.Workspaces.Commands.TransferOwnership;

namespace Notrelix.Application.Tests.Features.Workspaces.Workspaces.Commands;

public class TransferOwnershipCommandHandlerTests : WorkspaceHandlerTestBase
{
    private TransferOwnershipCommandHandler CreateSut() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, GrantProjectionMock.Object);

    [Fact]
    public async Task Handle_WhenWorkspaceAndMembersExist_TransfersOwnership()
    {
        var newOwnerId = Guid.CreateVersion7();
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers(
            CreateMember(WorkspaceRole.Owner),
            CreateMember(WorkspaceRole.Member, newOwnerId));
        var sut = CreateSut();
        var result = await sut.Handle(new TransferOwnershipCommand(TestWorkspaceId, newOwnerId, 1), CancellationToken.None);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenWorkspaceNotFound_ThrowsNotFoundException()
    {
        SetupWorkspaces();
        var sut = CreateSut();
        Func<Task> act = () => sut.Handle(new TransferOwnershipCommand(TestWorkspaceId, Guid.CreateVersion7(), 1), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenNewOwnerNotMember_ThrowsNotFoundException()
    {
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers(CreateMember(WorkspaceRole.Owner));
        var sut = CreateSut();
        Func<Task> act = () => sut.Handle(new TransferOwnershipCommand(TestWorkspaceId, Guid.CreateVersion7(), 1), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
