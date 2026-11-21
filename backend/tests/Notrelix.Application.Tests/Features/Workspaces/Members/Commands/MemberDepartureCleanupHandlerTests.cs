using Notrelix.Application.Features.Identity.Public.ApiTokens;
using Notrelix.Application.Features.Identity.Public.DefaultWorkspace;
using Notrelix.Application.Features.Workspaces.Members.Commands.LeaveWorkspace;
using Notrelix.Application.Features.Workspaces.Members.Commands.RemoveMember;

namespace Notrelix.Application.Tests.Features.Workspaces.Members.Commands;

/// <summary>
/// A membership that ends (a removal or a leave) deletes the person's User entries in Spaces and their Team
/// memberships in the same unit of work, in this Workspace only; the last Space Admin or Team Lead entry goes too.
/// </summary>
public class MemberDepartureCleanupHandlerTests : WorkspaceHandlerTestBase
{
    private readonly Guid _person = Guid.CreateVersion7();
    private readonly Guid _otherWorkspace = Guid.CreateVersion7();

    private LeaveWorkspaceCommandHandler Leave() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, GrantProjectionMock.Object,
        LockerMock.Object, new Mock<IIdentityDefaultWorkspace>().Object, new Mock<IIdentityApiTokens>().Object);

    private RemoveMemberCommandHandler Remove() => new(
        DbContextMock.Object, RequestContextMock.Object, DateTimeProviderMock.Object, GrantProjectionMock.Object, LockerMock.Object,
        new Mock<Notrelix.Application.Common.Realtime.IAccessChangeSignal>().Object, new Mock<IIdentityApiTokens>().Object);

    private (Space Own, Space OtherWorkspace, Space Untouched, Team OwnTeam, Team OtherTeam) Arrange()
    {
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        var own = CreateSpace();
        own.AddMember(SpaceMemberSubjectType.User, _person, SpaceRole.Admin, TestUserId, TestNow);
        var archived = CreateSpace();
        archived.AddMember(SpaceMemberSubjectType.User, _person, SpaceRole.Member, TestUserId, TestNow);
        archived.Archive(TestUserId, TestNow, SpaceContentFact.Empty);
        var untouched = CreateSpace();
        var foreign = Space.Create(TestAccountId, _otherWorkspace, "Elsewhere", SpaceVisibility.Workspace, _person, TestNow);

        var team = CreateTeam();
        team.AddMember(_person, TeamMemberRole.Lead, TestUserId, TestNow);
        var foreignTeam = Team.Create(TestAccountId, _otherWorkspace, "Elsewhere", _person, TestNow);
        foreignTeam.AddMember(_person, TeamMemberRole.Member, _person, TestNow);

        SetupSpaces(own, archived, untouched, foreign);
        SetupTeams(team, foreignTeam);
        return (own, foreign, untouched, team, foreignTeam);
    }

    private static bool InSpace(Space s, Guid user) => s.Members.Any(m => m.SubjectType == SpaceMemberSubjectType.User && m.SubjectId == user);
    private static bool InTeam(Team t, Guid user) => t.Members.Any(m => m.UserId == user && m.Status == TeamMemberStatus.Active);

    [Fact]
    public async Task ARemoval_DeletesThePersonsSpaceAndTeamEntries_InThisWorkspaceOnly()
    {
        var (own, foreign, untouched, team, foreignTeam) = Arrange();
        var member = CreateMember(userId: _person);
        SetupMembers(CreateMember(WorkspaceRole.Owner), member, CreateMember(WorkspaceRole.Owner, Guid.CreateVersion7()));

        await Remove().Handle(new RemoveMemberCommand(TestWorkspaceId, _person, member.Version), CancellationToken.None);

        InSpace(own, _person).Should().BeFalse("the sole Space Admin entry goes with the person (LD-12)");
        DbContextMock.Object.Spaces.Any(s => s.WorkspaceId == TestWorkspaceId && InSpace(s, _person)).Should().BeFalse("archived Spaces too");
        InTeam(team, _person).Should().BeFalse();
        team.Members.Any(m => m.Status == TeamMemberStatus.Active && m.Role == TeamMemberRole.Lead).Should().BeFalse("the sole Lead goes: the Team needs a lead");
        InSpace(untouched, TestUserId).Should().BeTrue("other people's entries stay");
        InSpace(foreign, _person).Should().BeTrue("another workspace is never touched");
        InTeam(foreignTeam, _person).Should().BeTrue("another workspace is never touched");
    }

    [Fact]
    public async Task ALeave_DeletesThePersonsSpaceAndTeamEntries_InThisWorkspaceOnly()
    {
        var (own, foreign, _, team, foreignTeam) = Arrange();
        RequestContextMock.Setup(r => r.UserId).Returns(_person);
        SetupMembers(CreateMember(WorkspaceRole.Member, _person), CreateMember(WorkspaceRole.Owner, Guid.CreateVersion7()));

        await Leave().Handle(new LeaveWorkspaceCommand(TestWorkspaceId), CancellationToken.None);

        InSpace(own, _person).Should().BeFalse();
        DbContextMock.Object.Spaces.Any(s => s.WorkspaceId == TestWorkspaceId && InSpace(s, _person)).Should().BeFalse();
        InTeam(team, _person).Should().BeFalse();
        InSpace(foreign, _person).Should().BeTrue();
        InTeam(foreignTeam, _person).Should().BeTrue();
    }

    [Fact]
    public async Task ARefusedLeave_ByTheLastOwner_CleansNothing()
    {
        var (own, _, _, team, _) = Arrange();
        RequestContextMock.Setup(r => r.UserId).Returns(_person);
        SetupMembers(CreateMember(WorkspaceRole.Owner, _person));

        var act = () => Leave().Handle(new LeaveWorkspaceCommand(TestWorkspaceId), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        InSpace(own, _person).Should().BeTrue();
        InTeam(team, _person).Should().BeTrue();
    }

    [Fact]
    public async Task APersonWithoutEntries_IsAFineLeave_AndNothingElseChanges()
    {
        SetupWorkspaces(CreateWorkspace(id: TestWorkspaceId));
        var space = CreateSpace();
        var version = space.Version;
        SetupSpaces(space);
        RequestContextMock.Setup(r => r.UserId).Returns(_person);
        SetupMembers(CreateMember(WorkspaceRole.Member, _person), CreateMember(WorkspaceRole.Owner, Guid.CreateVersion7()));

        var result = await Leave().Handle(new LeaveWorkspaceCommand(TestWorkspaceId), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        space.Version.Should().Be(version);
    }
}
