using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Workspaces.Teams.Commands.AddTeamMember;
using Notrelix.Application.Features.Workspaces.Teams.Commands.CreateTeam;
using Notrelix.Application.Features.Workspaces.Teams.Commands.DeleteTeam;
using Notrelix.Application.Features.Workspaces.Teams.Commands.RestoreTeam;
using Notrelix.Application.Features.Workspaces.Teams.Queries.GetDeletedTeams;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// The list of deleted Teams (UC-WSP-L09) through the real pipeline on PostgreSQL: an Owner or Admin lists the Teams
/// deleted in their Workspace (name, members that return, who deleted and when) and restores one from it; a Member or
/// Guest is refused, a person outside the Workspace is told it does not exist, another Workspace's deleted Teams are
/// never listed, and a restored Team leaves the list.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class DeletedTeamsIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public DeletedTeamsIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private Task<long> VersionAsync(Guid teamId) =>
        _host.ReadAsync(c => c.Teams.IgnoreQueryFilters().Where(t => t.Id == teamId).Select(t => t.Version).SingleAsync());

    /// <summary>Creates a Team as the Owner, adds one teammate, then deletes it; returns its id.</summary>
    private async Task<Guid> DeletedTeamAsync(OrgStack stack, string name, Guid? teammate = null)
    {
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var teamId = ((Result<Guid>)(await OrgTreeTestHost.SendAsync(owner, new CreateTeamCommand(stack.WorkspaceId, name, null)))!).Data;
        if (teammate is { } member)
            await OrgTreeTestHost.SendAsync(owner, new AddTeamMemberCommand(stack.WorkspaceId, teamId, member, "Member", await VersionAsync(teamId)));
        await OrgTreeTestHost.SendAsync(owner, new DeleteTeamCommand(stack.WorkspaceId, teamId, await VersionAsync(teamId)));
        return teamId;
    }

    private async Task<IReadOnlyList<DeletedTeamDto>> ListAsync(OrgStack stack, Guid userId)
    {
        using var provider = _host.CreateProvider(stack.AccountId, userId);
        return await OrgTreeTestHost.QueryAsync(provider, new GetDeletedTeamsQuery(stack.WorkspaceId));
    }

    [Fact]
    public async Task TheOwner_ListsTheDeletedTeams_NewestFirst_WithMembersAndWhoDeletedThem()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var teammate = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Teammate");
        var first = await DeletedTeamAsync(stack, "Old project", teammate);
        _host.Now = _host.Now.AddHours(1);
        var second = await DeletedTeamAsync(stack, "Support");

        var listed = await ListAsync(stack, stack.OwnerId);

        listed.Select(t => t.Id).Should().Equal(second, first);
        listed[1].Name.Should().Be("Old project");
        listed[1].MemberCount.Should().Be(2, "the Lead who made the Team and the teammate return on restore");
        listed[1].DeletedBy.Should().Be(stack.OwnerId);
        listed[1].DeletedByName.Should().Be("Org Owner");
    }

    [Fact]
    public async Task AnAdmin_ListsTheDeletedTeams()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin");
        var team = await DeletedTeamAsync(stack, "Old project");

        (await ListAsync(stack, admin)).Select(t => t.Id).Should().Equal(team);
    }

    [Theory]
    [InlineData(WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Guest)]
    public async Task AMemberOrGuest_IsRefused_AndSeesNoTeamName(WorkspaceRole role)
    {
        var stack = await _host.SeedWorkspaceAsync();
        var person = await _host.AddMemberAsync(stack, role, "Plain");
        await DeletedTeamAsync(stack, "Old project");
        using var provider = _host.CreateProvider(stack.AccountId, person);

        var read = () => OrgTreeTestHost.SendAsync(provider, new GetDeletedTeamsQuery(stack.WorkspaceId));

        await read.Should().ThrowAsync<AppForbidden>("restoring a Team is for Owners and Admins, so is the list of what can be restored");
    }

    [Fact]
    public async Task APersonOutsideTheWorkspace_IsToldItDoesNotExist()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var outsider = await _host.AddOutsiderAsync(stack);
        await DeletedTeamAsync(stack, "Old project");
        using var provider = _host.CreateProvider(stack.AccountId, outsider);

        var read = () => OrgTreeTestHost.SendAsync(provider, new GetDeletedTeamsQuery(stack.WorkspaceId));

        await read.Should().ThrowAsync<AppNotFound>();
    }

    [Fact]
    public async Task AnotherWorkspacesDeletedTeams_AreNeverListed_EvenToItsOwner()
    {
        var mine = await _host.SeedWorkspaceAsync();
        var other = await _host.SeedWorkspaceAsync();
        var mineDeleted = await DeletedTeamAsync(mine, "Mine");
        await DeletedTeamAsync(other, "Theirs");

        (await ListAsync(mine, mine.OwnerId)).Select(t => t.Id).Should().Equal(mineDeleted);
        // The Owner of one Workspace cannot read the other's list either: they are not a member there.
        using var provider = _host.CreateProvider(other.AccountId, mine.OwnerId);
        var read = () => OrgTreeTestHost.SendAsync(provider, new GetDeletedTeamsQuery(other.WorkspaceId));
        await read.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task ARestoredTeam_LeavesTheList()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var team = await DeletedTeamAsync(stack, "Old project");
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);

        await OrgTreeTestHost.SendAsync(owner, new RestoreTeamCommand(stack.WorkspaceId, team));

        (await ListAsync(stack, stack.OwnerId)).Should().BeEmpty();
    }
}
