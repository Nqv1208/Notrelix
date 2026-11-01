using Notrelix.Application.Features.Workspaces.Teams.Queries.GetWorkspaceTeamsExt;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// CTR-GAP-WS-TEAMS-EXT on PostgreSQL through the real pipeline (NRX-003, NRX-004, GOV-009, GOV-029):
/// every Team lists its people, who made and archived it and the Spaces where it holds a role;
/// the admins and the directory list active people only; a Guest or an outsider never reads it;
/// a Space the caller cannot see never appears in usage; nothing crosses Workspaces.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkspaceTeamsExtIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public WorkspaceTeamsExtIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Teams_ListPeopleTitlesCreatorArchiverAndSpaceUsage_WithDirectEntries()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var linh = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Linh");
        var hoa = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Hoa");
        var admin = await _host.AddMemberAsync(stack, WorkspaceRole.Admin, "Admin");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        await _host.WithSystemContextAsync(async c =>
        {
            c.WorkspaceMembers.Single(m => m.WorkspaceId == stack.WorkspaceId && m.UserId == linh)
                .ChangeJobTitle("Accountant", stack.OwnerId, OrgTreeTestHost.BaseTime);
            await c.SaveChangesAsync();
        });

        var active = Team.Create(stack.AccountId, stack.WorkspaceId, "Accounting", stack.OwnerId, OrgTreeTestHost.BaseTime);
        active.AddMember(linh, TeamMemberRole.Lead, stack.OwnerId, OrgTreeTestHost.BaseTime);
        active.AddMember(hoa, TeamMemberRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime.AddHours(1));
        var archived = Team.Create(stack.AccountId, stack.WorkspaceId, "Old team", stack.OwnerId, OrgTreeTestHost.BaseTime);
        archived.Archive(admin, OrgTreeTestHost.BaseTime.AddDays(3));
        await _host.WithSystemContextAsync(c => { c.Teams.Add(active); c.Teams.Add(archived); return Task.CompletedTask; });
        var department = await _host.SeedSpaceAsync(stack, "Finance", SpaceVisibility.Workspace, stack.OwnerId, type: SpaceType.Department, transition: s =>
        {
            s.AddMember(SpaceMemberSubjectType.Team, active.Id, SpaceRole.Viewer, stack.OwnerId, OrgTreeTestHost.BaseTime.AddDays(1));
            s.AddMember(SpaceMemberSubjectType.User, hoa, SpaceRole.Admin, stack.OwnerId, OrgTreeTestHost.BaseTime.AddDays(1));
            s.AddMember(SpaceMemberSubjectType.User, guest, SpaceRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime.AddDays(1));
        });
        using var asMember = _host.CreateProvider(stack.AccountId, hoa);

        var ext = await OrgTreeTestHost.QueryAsync(asMember, new GetWorkspaceTeamsExtQuery(stack.WorkspaceId));

        var team = ext.Teams.Should().HaveCount(2).And.Subject.Single(t => t.Id == active.Id);
        team.CreatedBy!.Id.Should().Be(stack.OwnerId);
        team.ArchivedAt.Should().BeNull();
        team.ArchivedBy.Should().BeNull();
        team.People.Select(p => p.UserId).Should().Equal(linh, hoa);
        team.People[0].Role.Should().Be("Lead");
        team.People[0].Title.Should().Be("Accountant");
        team.People.Should().OnlyContain(p => p.Email == null);
        var usage = team.Usage.Should().ContainSingle().Subject;
        usage.Kind.Should().Be("space");
        usage.Id.Should().Be(department);
        usage.SpaceType.Should().Be("Department");
        usage.Grant.Should().Be("Viewer");
        usage.Direct.Should().ContainSingle(d => d.UserId == hoa && d.Role == "Admin", "a Team person with a direct entry keeps access if the Team stops granting");
        usage.Direct.Should().NotContain(d => d.UserId == guest, "only people of the Team are listed");

        var old = ext.Teams.Single(t => t.Id == archived.Id);
        old.ArchivedAt.Should().Be(OrgTreeTestHost.BaseTime.AddDays(3));
        old.ArchivedBy!.Id.Should().Be(admin);
        old.Usage.Should().BeEmpty();

        ext.Admins.Select(a => (a.Id, a.Role)).Should().BeEquivalentTo([(stack.OwnerId, "owner"), (admin, "admin")]);
        ext.Directory.Select(d => d.Id).Should().BeEquivalentTo([stack.OwnerId, linh, hoa, admin]);
        ext.Directory.Should().NotContain(d => d.Id == guest, "a Guest is not a person a Team may take");
        ext.Directory.Single(d => d.Id == linh).Title.Should().Be("Accountant");
    }

    [Fact]
    public async Task Usage_NeverListsASpaceTheCallerCannotSee()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var team = Team.Create(stack.AccountId, stack.WorkspaceId, "Secret team", stack.OwnerId, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(c => { c.Teams.Add(team); return Task.CompletedTask; });
        await _host.SeedSpaceAsync(stack, "Board of directors", SpaceVisibility.Private, stack.OwnerId, transition: s =>
            s.AddMember(SpaceMemberSubjectType.Team, team.Id, SpaceRole.Admin, stack.OwnerId, OrgTreeTestHost.BaseTime));
        using var asMember = _host.CreateProvider(stack.AccountId, member);
        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);

        (await OrgTreeTestHost.QueryAsync(asMember, new GetWorkspaceTeamsExtQuery(stack.WorkspaceId)))
            .Teams.Single().Usage.Should().BeEmpty("a Private Space the member cannot see does not leak through Team usage");
        (await OrgTreeTestHost.QueryAsync(asOwner, new GetWorkspaceTeamsExtQuery(stack.WorkspaceId)))
            .Teams.Single().Usage.Should().ContainSingle();
    }

    [Fact]
    public async Task GuestAndOutsiderAreForbidden_AndAnotherWorkspaceIsNeverListed()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var other = await _host.SeedWorkspaceAsync();
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var outsider = await _host.AddOutsiderAsync(stack);
        var foreign = Team.Create(other.AccountId, other.WorkspaceId, "Foreign team", other.OwnerId, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(c => { c.Teams.Add(foreign); return Task.CompletedTask; });

        foreach (var userId in new[] { guest, outsider })
        {
            using var provider = _host.CreateProvider(stack.AccountId, userId);
            var read = () => OrgTreeTestHost.QueryAsync(provider, new GetWorkspaceTeamsExtQuery(stack.WorkspaceId));
            if (userId == outsider)
                await read.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.NotFoundException>("a person who is not an active member is told the workspace does not exist");
            else
                await read.Should().ThrowAsync<AppForbidden>();
        }

        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var ext = await OrgTreeTestHost.QueryAsync(asOwner, new GetWorkspaceTeamsExtQuery(stack.WorkspaceId));
        ext.Teams.Should().BeEmpty("a Team of another Workspace is never listed");
        ext.Directory.Select(d => d.Id).Should().Equal(stack.OwnerId);
    }
}
