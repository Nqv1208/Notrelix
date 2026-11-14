using Notrelix.Application.Features.Workspaces.Teams.Services;

namespace Notrelix.Application.Tests.Features.Workspaces.Teams;

public class TeamMembershipFactsTests : WorkspaceHandlerTestBase
{
    private readonly Guid _alice = Guid.CreateVersion7();
    private readonly Guid _bob = Guid.CreateVersion7();
    private readonly Guid _carol = Guid.CreateVersion7();
    private readonly Guid _dave = Guid.CreateVersion7();

    private TeamMembershipFacts Facts => new(DbContextMock.Object);

    [Fact]
    public async Task CurrentMemberIds_lists_only_active_team_members_who_are_active_workspace_members()
    {
        var team = CreateTeam();
        var removed = CreateTeamMember(team.Id, userId: _bob);
        removed.Remove(TestUserId, TestNow);
        SetupTeams(team);
        SetupMembers(CreateMember(userId: _alice), CreateMember(userId: _bob), CreateMember(userId: _dave));
        // Carol is on the Team but no longer a Workspace member; Bob left the Team; Dave never joined it.
        SetupTeamMembers(CreateTeamMember(team.Id, userId: _alice), removed, CreateTeamMember(team.Id, userId: _carol));

        var ids = await Facts.CurrentMemberIdsAsync(TestWorkspaceId, team.Id, CancellationToken.None);

        ids.Should().BeEquivalentTo([_alice]);
    }

    [Fact]
    public async Task CurrentMemberIds_is_empty_for_an_archived_team()
    {
        var team = CreateTeam();
        team.Archive(TestUserId, TestNow);
        SetupTeams(team);
        SetupMembers(CreateMember(userId: _alice));
        SetupTeamMembers(CreateTeamMember(team.Id, userId: _alice));

        var ids = await Facts.CurrentMemberIdsAsync(TestWorkspaceId, team.Id, CancellationToken.None);

        ids.Should().BeEmpty();
    }

    [Fact]
    public async Task CurrentMemberIds_never_reaches_a_team_of_another_workspace()
    {
        var team = CreateTeam();
        SetupTeams(team);
        SetupMembers(CreateMember(userId: _alice));
        SetupTeamMembers(CreateTeamMember(team.Id, userId: _alice));

        var ids = await Facts.CurrentMemberIdsAsync(Guid.CreateVersion7(), team.Id, CancellationToken.None);

        ids.Should().BeEmpty();
    }
}
