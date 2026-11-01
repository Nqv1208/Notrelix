using Notrelix.Application.Features.Workspaces.Members.Commands.SuspendMember;

namespace Notrelix.Application.Tests.Features.Workspaces.Members.Commands;

public class SuspendMemberCommandHandlerTests : WorkspaceHandlerTestBase
{
    private readonly Guid _targetId = Guid.CreateVersion7();

    private SuspendMemberCommandHandler CreateSut() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, GrantProjectionMock.Object, LockerMock.Object,
        new Mock<Notrelix.Application.Common.Realtime.IAccessChangeSignal>().Object);

    [Fact]
    public async Task Handle_WhenMemberExists_SuspendsSuccessfully()
    {
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        var member = CreateMember(userId: _targetId);
        SetupMembers(CreateMember(WorkspaceRole.Owner), member);
        var sut = CreateSut();
        var result = await sut.Handle(new SuspendMemberCommand(TestWorkspaceId, _targetId, member.Version), CancellationToken.None);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenMemberNotFound_ThrowsNotFoundException()
    {
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        SetupMembers();
        var sut = CreateSut();
        Func<Task> act = () => sut.Handle(new SuspendMemberCommand(TestWorkspaceId, TestUserId, 1), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenMemberSuspended_RevokesWorkspaceGrant()
    {
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        var member = CreateMember(userId: _targetId);
        SetupMembers(CreateMember(WorkspaceRole.Owner), member);
        var sut = CreateSut();

        await sut.Handle(new SuspendMemberCommand(TestWorkspaceId, _targetId, member.Version), CancellationToken.None);

        GrantProjectionMock.Verify(
            p => p.RevokeWorkspaceMemberGrantAsync(
                TestAccountId, TestWorkspaceId, _targetId, TestNow, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTheActorIsTheTarget_IsRefusedBeforeAnyChange()
    {
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        var self = CreateMember(WorkspaceRole.Owner);
        SetupMembers(self, CreateMember(WorkspaceRole.Owner, _targetId));

        var act = () => CreateSut().Handle(new SuspendMemberCommand(TestWorkspaceId, TestUserId, self.Version), CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>()).Which.RuleCode
            .Should().Be(Notrelix.Domain.Workspaces.WorkspaceRuleCodes.Workspaces_Member_CannotActOnSelf);
        self.Status.Should().Be(Notrelix.Domain.Workspaces.Members.WorkspaceMemberStatus.Active);
        GrantProjectionMock.Verify(
            p => p.RevokeWorkspaceMemberGrantAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
