using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceMembersExtra;
using Notrelix.Application.Features.Workspaces.Teams.Queries.GetTeamMembers;
using Notrelix.Application.Features.Workspaces.Teams.Queries.GetWorkspaceTeamsExt;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// Member email (accepted 2026-10-23): a Workspace Owner or Admin sees the email of active non-Guest
/// members, any other member sees none, a Guest never sees the list and a Guest's address is never
/// shown. The address comes from the Identity public facts port; one policy serves every member read.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class MemberEmailVisibilityIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public MemberEmailVisibilityIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.ConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task TeamsExtAndTeamMembers_ShowEmailToOwnerAndAdminOnly_AndNeverOfAGuest()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin");
        var linh = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Linh");
        var plain = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Plain");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var team = Team.Create(stack.AccountId, stack.WorkspaceId, "Accounting", stack.OwnerId, OrgTreeTestHost.BaseTime);
        team.AddMember(linh, TeamMemberRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
        team.AddMember(guest, TeamMemberRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(c => { c.Teams.Add(team); return Task.CompletedTask; });

        foreach (var reader in new[] { stack.OwnerId, admin })
        {
            using var provider = _host.CreateProvider(stack.AccountId, reader);
            var ext = await OrgTreeTestHost.QueryAsync(provider, new GetWorkspaceTeamsExtQuery(stack.WorkspaceId));
            var people = ext.Teams.Single().People;
            people.Single(p => p.UserId == linh).Email.Should().NotBeNullOrEmpty();
            people.Single(p => p.UserId == guest).Email.Should().BeNull("a Guest's email is never shown");
            ext.Admins.Should().OnlyContain(a => !string.IsNullOrEmpty(a.Email));
            ext.Directory.Should().OnlyContain(d => !string.IsNullOrEmpty(d.Email));
            ext.Directory.Should().NotContain(d => d.Id == guest);

            var members = await OrgTreeTestHost.SendAsync(provider, new GetTeamMembersQuery(stack.WorkspaceId, team.Id));
            var list = (IReadOnlyList<TeamMemberDto>)members!;
            list.Single(m => m.UserId == linh).Email.Should().NotBeNullOrEmpty();
            list.Single(m => m.UserId == guest).Email.Should().BeNull();
        }

        using var asPlain = _host.CreateProvider(stack.AccountId, plain);
        var plainExt = await OrgTreeTestHost.QueryAsync(asPlain, new GetWorkspaceTeamsExtQuery(stack.WorkspaceId));
        plainExt.Teams.Single().People.Should().OnlyContain(p => p.Email == null);
        plainExt.Admins.Should().OnlyContain(a => a.Email == null);
        plainExt.Directory.Should().OnlyContain(d => d.Email == null);
        var plainMembers = (IReadOnlyList<TeamMemberDto>)(await OrgTreeTestHost.SendAsync(asPlain, new GetTeamMembersQuery(stack.WorkspaceId, team.Id)))!;
        plainMembers.Should().OnlyContain(m => m.Email == null);

        using var asGuest = _host.CreateProvider(stack.AccountId, guest);
        var guestRead = () => OrgTreeTestHost.QueryAsync(asGuest, new GetWorkspaceTeamsExtQuery(stack.WorkspaceId));
        await guestRead.Should().ThrowAsync<AppForbidden>("a Guest does not enumerate the Workspace");
    }

    [Fact]
    public async Task SpaceMembersExtra_ShowsEmailToOwnerAndAdminOnly()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin");
        var hoa = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Hoa");
        var space = await _host.SeedSpaceAsync(stack, "Finance", SpaceVisibility.Workspace, stack.OwnerId, type: SpaceType.Department, transition: s =>
            s.AddMember(SpaceMemberSubjectType.User, hoa, SpaceRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime));

        foreach (var reader in new[] { stack.OwnerId, admin })
        {
            using var provider = _host.CreateProvider(stack.AccountId, reader);
            var extra = await OrgTreeTestHost.QueryAsync(provider, new GetSpaceMembersExtraQuery(stack.WorkspaceId, space));
            extra.People.Single(p => p.UserId == hoa).Email.Should().NotBeNullOrEmpty();
        }

        using var asMember = _host.CreateProvider(stack.AccountId, hoa);
        var memberExtra = await OrgTreeTestHost.QueryAsync(asMember, new GetSpaceMembersExtraQuery(stack.WorkspaceId, space));
        memberExtra.People.Should().OnlyContain(p => p.Email == null);
    }

    [Fact]
    public async Task EmailOfAnotherWorkspacesMember_IsNeverResolved_ForAnOwnerOfThisOne()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var other = await _host.SeedWorkspaceAsync();
        var foreignMember = await _host.AddMemberAsync(other, WorkspaceRole.Member, "Foreign");
        var team = Team.Create(stack.AccountId, stack.WorkspaceId, "Accounting", stack.OwnerId, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(c => { c.Teams.Add(team); return Task.CompletedTask; });

        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var ext = await OrgTreeTestHost.QueryAsync(asOwner, new GetWorkspaceTeamsExtQuery(stack.WorkspaceId));

        ext.Directory.Select(d => d.Id).Should().NotContain(foreignMember);
        ext.Directory.Should().OnlyContain(d => d.Id == stack.OwnerId);
    }
}
