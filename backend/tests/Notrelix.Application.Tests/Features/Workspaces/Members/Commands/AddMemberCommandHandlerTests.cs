using Notrelix.Application.Features.Workspaces.Members.Commands.AddMember;

namespace Notrelix.Application.Tests.Features.Workspaces.Members.Commands;

public class AddMemberCommandHandlerTests : WorkspaceHandlerTestBase
{
    private AddMemberCommandHandler CreateSut() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object);

    [Fact]
    public async Task Handle_WhenWorkspaceExists_AddsMemberSuccessfully()
    {
        var workspace = CreateWorkspace();
        SetupWorkspaces(workspace);
        SetupMembers();
        var sut = CreateSut();
        var command = new AddMemberCommand(workspace.Id, Guid.CreateVersion7(), WorkspaceRole.Member);
        var result = await sut.Handle(command, CancellationToken.None);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenWorkspaceNotFound_ThrowsNotFoundException()
    {
        SetupWorkspaces();
        var sut = CreateSut();
        var command = new AddMemberCommand(TestWorkspaceId, Guid.CreateVersion7(), WorkspaceRole.Member);
        Func<Task> act = () => sut.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
