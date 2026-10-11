using Notrelix.Application.Features.Workspaces.Teams.Queries.GetTeam;
using Notrelix.Application.Features.Workspaces.Teams.Queries.GetTeamMembers;
using Notrelix.Application.Features.Workspaces.Teams.Queries.GetWorkspaceTeams;

namespace Notrelix.Application.Tests.Features.Workspaces.Teams.Queries;

/// <summary>
/// Read contract of a Team's member list and the Team reads that summarise it:
/// only active members are listed (a removed member no longer participates),
/// the Team read's member count agrees with that list, and a Team of another
/// workspace is not found. PostgreSQL behavior is proven by
/// TeamAdministrationReadIntegrationTests.
/// </summary>
public sealed class GetTeamMembersQueryHandlerTests : WorkspaceHandlerTestBase
{
    private readonly Mock<IActorLookupService> _actors = new();

    public GetTeamMembersQueryHandlerTests()
    {
        _actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Guid> ids, CancellationToken _) =>
                ids.Select(id => new ActorSnapshot(id, $"User {id:N}", null)).ToList());
    }

    [Fact]
    public async Task GetTeamMembers_ReturnsActiveMembersWithRole()
    {
        var team = CreateTeam();
        var lead = Guid.CreateVersion7();
        var member = Guid.CreateVersion7();
        team.AddMember(lead, TeamMemberRole.Lead, TestUserId, TestNow);
        team.AddMember(member, TeamMemberRole.Member, TestUserId, TestNow);
        SetupTeams(team);
        SetupTeamMembers(team.Members.ToArray());

        var members = await CreateMembersSut()
            .Handle(new GetTeamMembersQuery(TestWorkspaceId, team.Id), CancellationToken.None);

        members.Select(m => (m.UserId, m.Role, m.Status)).Should().BeEquivalentTo(new[]
        {
            (lead, "Lead", "Active"),
            (member, "Member", "Active"),
        });
        members.Should().OnlyContain(m => m.DisplayName.StartsWith("User "));
    }

    [Fact]
    public async Task GetTeamMembers_ExcludesRemovedMembers_AndCountMatches()
    {
        var team = CreateTeam();
        var stays = Guid.CreateVersion7();
        var leaves = Guid.CreateVersion7();
        team.AddMember(stays, TeamMemberRole.Member, TestUserId, TestNow);
        team.AddMember(leaves, TeamMemberRole.Member, TestUserId, TestNow);
        team.RemoveMember(leaves, TestUserId, TestNow.AddMinutes(1));
        SetupTeams(team);
        SetupTeamMembers(team.Members.ToArray());

        var members = await CreateMembersSut()
            .Handle(new GetTeamMembersQuery(TestWorkspaceId, team.Id), CancellationToken.None);
        var single = await new GetTeamQueryHandler(DbContextMock.Object)
            .Handle(new GetTeamQuery(TestWorkspaceId, team.Id), CancellationToken.None);
        var listed = await new GetWorkspaceTeamsQueryHandler(DbContextMock.Object)
            .Handle(new GetWorkspaceTeamsQuery(TestWorkspaceId), CancellationToken.None);

        members.Select(m => m.UserId).Should().Equal(stays);
        single.Data!.MemberCount.Should().Be(members.Count, "a removed member no longer participates");
        listed.Data!.Single().MemberCount.Should().Be(members.Count);
        single.Data.Version.Should().Be(team.Version).And.BeGreaterThan(1, "membership changes advance the version");
        single.Data.Status.Should().Be("Active");
        listed.Data.Single().Version.Should().Be(team.Version);
    }

    [Fact]
    public async Task GetTeamMembers_ReportsArchivedStatusOnTheTeamReads()
    {
        var team = CreateTeam();
        team.Archive(TestUserId, TestNow.AddMinutes(1));
        SetupTeams(team);

        var single = await new GetTeamQueryHandler(DbContextMock.Object)
            .Handle(new GetTeamQuery(TestWorkspaceId, team.Id), CancellationToken.None);
        var listed = await new GetWorkspaceTeamsQueryHandler(DbContextMock.Object)
            .Handle(new GetWorkspaceTeamsQuery(TestWorkspaceId), CancellationToken.None);

        single.Data!.Status.Should().Be("Archived");
        listed.Data!.Single().Status.Should().Be("Archived");
    }

    [Fact]
    public async Task GetTeamMembers_OtherWorkspace_IsNotFound()
    {
        var team = CreateTeam();
        team.AddMember(Guid.CreateVersion7(), TeamMemberRole.Member, TestUserId, TestNow);
        SetupTeams(team);
        SetupTeamMembers(team.Members.ToArray());

        var act = () => CreateMembersSut()
            .Handle(new GetTeamMembersQuery(Guid.CreateVersion7(), team.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetTeamMembers_DeletedTeam_IsNotFound()
    {
        var team = CreateTeam();
        team.Delete(TestUserId, TestNow.AddMinutes(1));
        SetupTeams(team);

        var act = () => CreateMembersSut()
            .Handle(new GetTeamMembersQuery(TestWorkspaceId, team.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private GetTeamMembersQueryHandler CreateMembersSut() => new(DbContextMock.Object, _actors.Object);
}
