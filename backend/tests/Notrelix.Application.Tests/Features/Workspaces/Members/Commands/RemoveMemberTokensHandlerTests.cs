using Notrelix.Application.Features.Identity.Public.ApiTokens;
using Notrelix.Application.Features.Workspaces.Members.Commands.RemoveMember;
using Notrelix.Domain.Identity.Tokens;

namespace Notrelix.Application.Tests.Features.Workspaces.Members.Commands;

/// <summary>Removing a member ends the tokens they issued in that workspace, in the same unit of work.</summary>
public class RemoveMemberTokensHandlerTests : WorkspaceHandlerTestBase
{
    private readonly Guid _targetId = Guid.CreateVersion7();
    private readonly Mock<IIdentityApiTokens> _apiTokens = new();

    private RemoveMemberCommandHandler Sut() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, GrantProjectionMock.Object, LockerMock.Object,
        new Mock<Notrelix.Application.Common.Realtime.IAccessChangeSignal>().Object, _apiTokens.Object);

    [Fact]
    public async Task Handle_WhenMemberRemoved_StopsTheirTokensInThatWorkspace()
    {
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        var member = CreateMember(userId: _targetId);
        SetupMembers(CreateMember(WorkspaceRole.Owner), member, CreateMember(WorkspaceRole.Owner, Guid.CreateVersion7()));

        var result = await Sut().Handle(new RemoveMemberCommand(TestWorkspaceId, _targetId, member.Version), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _apiTokens.Verify(
            t => t.RevokeForMemberAsync(_targetId, TestWorkspaceId, TestUserId,
                ApiTokenRevokedReason.MemberRemoved, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTheActorIsTheTarget_StopsNoTokens()
    {
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        var self = CreateMember(WorkspaceRole.Owner);
        SetupMembers(self, CreateMember(WorkspaceRole.Owner, _targetId));

        var act = () => Sut().Handle(new RemoveMemberCommand(TestWorkspaceId, TestUserId, self.Version), CancellationToken.None);
        await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();

        _apiTokens.Verify(
            t => t.RevokeForMemberAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<ApiTokenRevokedReason>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
