using Notrelix.Application.Features.Identity.Public.DefaultWorkspace;
using Notrelix.Application.Features.Workspaces.Members.Commands.LeaveWorkspace;
using Notrelix.Application.Features.Workspaces.Members.Commands.SetDefaultWorkspace;
using Notrelix.Domain.Workspaces;

namespace Notrelix.Application.Tests.Features.Workspaces.Members.Commands;

/// <summary>Leaving a Workspace and choosing the default Workspace: the principal's own membership only.</summary>
public class LeaveWorkspaceHandlerTests : WorkspaceHandlerTestBase
{
    private readonly Mock<IIdentityDefaultWorkspace> _defaults = new();

    private LeaveWorkspaceCommandHandler Leave() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object,
        GrantProjectionMock.Object, LockerMock.Object, _defaults.Object);

    private SetDefaultWorkspaceCommandHandler SetDefault() => new(
        DbContextMock.Object, RequestContextMock.Object, _defaults.Object);

    private void Arrange(WorkspaceRole myRole, int otherActiveOwners)
    {
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        var members = new List<WorkspaceMember> { CreateMember(myRole) };
        for (var i = 0; i < otherActiveOwners; i++)
        {
            members.Add(CreateMember(WorkspaceRole.Owner, Guid.CreateVersion7()));
        }

        SetupMembers(members.ToArray());
    }

    [Theory]
    [InlineData(WorkspaceRole.Guest)]
    [InlineData(WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Admin)]
    public async Task AMemberWhoIsNotAnOwner_Leaves_RevokesTheGrant_AndClearsTheDefault(WorkspaceRole role)
    {
        Arrange(role, otherActiveOwners: 1);

        var result = await Leave().Handle(new LeaveWorkspaceCommand(TestWorkspaceId), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        DbContextMock.Object.WorkspaceMembers.Single(m => m.UserId == TestUserId).Status.Should().Be(WorkspaceMemberStatus.Removed);
        GrantProjectionMock.Verify(g => g.RevokeLeavingMemberGrantAsync(
            TestAccountId, TestWorkspaceId, TestUserId, TestNow, It.IsAny<CancellationToken>()), Times.Once);
        _defaults.Verify(d => d.ClearIfAsync(TestUserId, TestWorkspaceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AnOwner_Leaves_WhenAnotherActiveOwnerRemains()
    {
        Arrange(WorkspaceRole.Owner, otherActiveOwners: 1);

        var result = await Leave().Handle(new LeaveWorkspaceCommand(TestWorkspaceId), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task TheLastOwner_CannotLeave_ConflictWithADottedCode_AndNothingChanges()
    {
        Arrange(WorkspaceRole.Owner, otherActiveOwners: 0);
        var me = DbContextMock.Object.WorkspaceMembers.Single();
        var version = me.Version;

        var act = () => Leave().Handle(new LeaveWorkspaceCommand(TestWorkspaceId), CancellationToken.None);

        var thrown = (await act.Should().ThrowAsync<ConflictException>()).Which;
        thrown.ErrorCode.Should().Be("workspaces.member.last-owner-cannot-leave");
        me.Status.Should().Be(WorkspaceMemberStatus.Active);
        me.Version.Should().Be(version);
        GrantProjectionMock.Verify(g => g.RevokeLeavingMemberGrantAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
        _defaults.Verify(d => d.ClearIfAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ASuspendedOwnerDoesNotCountAsAnotherOwner()
    {
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        var suspended = CreateMember(WorkspaceRole.Owner, Guid.CreateVersion7());
        suspended.Suspend(TestUserId, TestNow, 2);
        SetupMembers(CreateMember(WorkspaceRole.Owner), suspended);

        var act = () => Leave().Handle(new LeaveWorkspaceCommand(TestWorkspaceId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task OnlyThePrincipalsOwnMembershipIsEverRemoved()
    {
        Arrange(WorkspaceRole.Member, otherActiveOwners: 1);
        var others = DbContextMock.Object.WorkspaceMembers.Where(m => m.UserId != TestUserId).ToList();

        await Leave().Handle(new LeaveWorkspaceCommand(TestWorkspaceId), CancellationToken.None);

        others.Should().OnlyContain(m => m.Status == WorkspaceMemberStatus.Active);
    }

    [Fact]
    public async Task ALeaver_WhoIsNotAnActiveMember_IsNotFound_AndNothingIsRevoked()
    {
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        var removed = CreateMember(WorkspaceRole.Member);
        removed.Remove(2, TestUserId, TestNow);
        SetupMembers(removed, CreateMember(WorkspaceRole.Owner, Guid.CreateVersion7()));

        var act = () => Leave().Handle(new LeaveWorkspaceCommand(TestWorkspaceId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        GrantProjectionMock.Verify(g => g.RevokeLeavingMemberGrantAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ALeaver_OfAWorkspaceThatIsNotThere_IsNotFound()
    {
        SetupWorkspaces();
        SetupMembers(CreateMember(WorkspaceRole.Member));

        var act = () => Leave().Handle(new LeaveWorkspaceCommand(TestWorkspaceId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SetDefault_ForAnActiveMember_ChoosesTheWorkspace()
    {
        Arrange(WorkspaceRole.Guest, otherActiveOwners: 1);

        var result = await SetDefault().Handle(new SetDefaultWorkspaceCommand(TestWorkspaceId), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _defaults.Verify(d => d.SetAsync(TestUserId, TestWorkspaceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetDefault_ForARemovedOrSuspendedOrAbsentMember_IsNotFound_AndNothingIsStored()
    {
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        var suspended = CreateMember(WorkspaceRole.Member);
        suspended.Suspend(TestUserId, TestNow, 2);
        SetupMembers(suspended, CreateMember(WorkspaceRole.Member, Guid.CreateVersion7()));

        var act = () => SetDefault().Handle(new SetDefaultWorkspaceCommand(TestWorkspaceId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _defaults.Verify(d => d.SetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
