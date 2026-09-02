using Notrelix.Application.Features.Workspaces.Members.Commands.AddMember;

namespace Notrelix.Application.Tests.Features.Workspaces.Members.Commands;

public class AddMemberCommandHandlerTests : WorkspaceHandlerTestBase
{
    private readonly Mock<IActorLookupService> _actorLookupMock = new();

    private AddMemberCommandHandler CreateSut() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, GrantProjectionMock.Object, _actorLookupMock.Object);

    [Fact]
    public async Task Handle_WhenWorkspaceExists_AddsMemberSuccessfully()
    {
        var workspace = CreateWorkspace(TestWorkspaceId);
        SetupWorkspaces(workspace);
        SetupMembers();
        SetupActorFound();
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
    public async Task Handle_WhenTargetUserDoesNotExist_ThrowsNotFoundExceptionAndSyncsNoGrant()
    {
        var workspace = CreateWorkspace(TestWorkspaceId);
        SetupWorkspaces(workspace);
        SetupMembers();
        ActorLookupNull();
        var sut = CreateSut();
        var unknownUserId = Guid.CreateVersion7();
        var command = new AddMemberCommand(workspace.Id, unknownUserId, WorkspaceRole.Member);

        var act = () => sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        GrantProjectionMock.Verify(
            p => p.SyncWorkspaceMemberGrantAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkspaceRole>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMemberAdded_SyncsWorkspaceGrantForNewMember()
    {
        var workspace = CreateWorkspace(TestWorkspaceId);
        SetupWorkspaces(workspace);
        SetupMembers();
        SetupActorFound();
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
    public async Task Handle_WhenExistingSuspendedMemberExists_ReactivatesWithRequestedRole()
    {
        var workspace = CreateWorkspace(TestWorkspaceId);
        SetupWorkspaces(workspace);
        var suspendedMember = CreateMember(WorkspaceRole.Member);
        suspendedMember.Suspend(TestUserId, TestNow, 2);
        SetupMembers(suspendedMember);
        SetupActorFound();
        var sut = CreateSut();
        var command = new AddMemberCommand(workspace.Id, TestUserId, WorkspaceRole.Admin);

        var result = await sut.Handle(command, CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        GrantProjectionMock.Verify(
            p => p.SyncWorkspaceMemberGrantAsync(
                workspace.AccountId, workspace.Id, TestUserId, WorkspaceRole.Admin, TestNow, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExistingMemberWasRemoved_ThrowsCannotActivateRemoved()
    {
        var workspace = CreateWorkspace(TestWorkspaceId);
        SetupWorkspaces(workspace);
        var removedMember = CreateMember(WorkspaceRole.Member);
        removedMember.Remove(2, TestUserId, TestNow.AddDays(1));
        SetupMembers(removedMember);
        SetupActorFound();
        var sut = CreateSut();
        var command = new AddMemberCommand(workspace.Id, TestUserId, WorkspaceRole.Member);

        var act = () => sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>()
            .WithMessage("*Cannot activate a removed member*");
        GrantProjectionMock.Verify(
            p => p.SyncWorkspaceMemberGrantAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<WorkspaceRole>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void SetupActorFound()
    {
        _actorLookupMock.Setup(x => x.FindAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userId, CancellationToken _) => new ActorSnapshot(userId, "Target User", null));
    }

    private void ActorLookupNull()
    {
        _actorLookupMock.Setup(x => x.FindAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActorSnapshot?)null);
    }
}