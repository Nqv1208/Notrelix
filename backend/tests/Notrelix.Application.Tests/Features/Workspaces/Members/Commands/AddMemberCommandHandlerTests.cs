using Notrelix.Application.Features.Workspaces.Members.Commands.AddMember;

namespace Notrelix.Application.Tests.Features.Workspaces.Members.Commands;

public class AddMemberCommandHandlerTests : WorkspaceHandlerTestBase
{
    private AddMemberCommandHandler CreateSut() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, GrantProjectionMock.Object);

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

    [Fact]
    public async Task Handle_WhenMemberAdded_SyncsWorkspaceGrantForNewMember()
    {
        var workspace = CreateWorkspace();
        SetupWorkspaces(workspace);
        SetupMembers();
        var sut = CreateSut();
        var newUserId = Guid.CreateVersion7();
        var command = new AddMemberCommand(workspace.Id, newUserId, WorkspaceRole.Member);

        await sut.Handle(command, CancellationToken.None);

        GrantProjectionMock.Verify(
            p => p.SyncWorkspaceMemberGrantAsync(
                workspace.AccountId, workspace.Id, newUserId, WorkspaceRole.Member, TestNow, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenReactivatingExistingMember_SyncsWorkspaceGrantWithRequestedRole()
    {
        var workspace = CreateWorkspace();
        SetupWorkspaces(workspace);
        var removedMember = CreateMember(WorkspaceRole.Member);
        removedMember.Remove(1, TestUserId, TestNow.AddDays(1));
        SetupMembers(removedMember);
        var sut = CreateSut();
        var command = new AddMemberCommand(workspace.Id, TestUserId, WorkspaceRole.Admin);

        var result = await sut.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        GrantProjectionMock.Verify(
            p => p.SyncWorkspaceMemberGrantAsync(
                workspace.AccountId, workspace.Id, TestUserId, WorkspaceRole.Admin, TestNow, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
