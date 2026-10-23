using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceMembersExtra;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// CTR-GAP-WS-MEMBERS-EXT on PostgreSQL through the real pipeline (NRX-003, NRX-004, GOV-009,
/// GOV-029, WSP-026): the summary counts each reached person once at the highest role with Teams
/// expanded; a pending invitation naming the Space is listed and counted until it is accepted or
/// revoked; a Space the caller cannot see is NotFound; a Guest or outsider is refused; another
/// Workspace's Space is never reachable.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class SpaceMembersExtraIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public SpaceMembersExtraIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.ConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Summary_CountsEachPersonOnceAtTheHighestRole_WithTeamsExpandedAndViaTeamsListed()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var linh = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Linh");
        var hoa = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Hoa");
        var minh = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Minh");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var team = Team.Create(stack.AccountId, stack.WorkspaceId, "Accounting", stack.OwnerId, OrgTreeTestHost.BaseTime);
        team.AddMember(linh, TeamMemberRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
        team.AddMember(hoa, TeamMemberRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(c => { c.Teams.Add(team); return Task.CompletedTask; });
        var space = await _host.SeedSpaceAsync(stack, "Finance", SpaceVisibility.Workspace, stack.OwnerId, type: SpaceType.Department, transition: s =>
        {
            s.AddMember(SpaceMemberSubjectType.Team, team.Id, SpaceRole.Viewer, stack.OwnerId, OrgTreeTestHost.BaseTime);
            s.AddMember(SpaceMemberSubjectType.User, hoa, SpaceRole.Admin, stack.OwnerId, OrgTreeTestHost.BaseTime);
            s.AddMember(SpaceMemberSubjectType.User, minh, SpaceRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
            s.AddMember(SpaceMemberSubjectType.User, guest, SpaceRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
        });
        using var asMember = _host.CreateProvider(stack.AccountId, minh);

        var extra = await OrgTreeTestHost.QueryAsync(asMember, new GetSpaceMembersExtraQuery(stack.WorkspaceId, space));

        // Owner (creator, Admin) + Hoa (Admin beats the Team's Viewer) = 2 admins; Minh = 1 member; Linh = 1 viewer; the Guest is not a person of the Space.
        extra.Summary.Should().Be(new SpaceMembersExtraSummaryDto(2, 1, 1, 0));
        extra.People.Select(p => p.UserId).Should().BeEquivalentTo([stack.OwnerId, hoa, minh, linh]);
        extra.People.Should().NotContain(p => p.UserId == guest);
        extra.People.Single(p => p.UserId == hoa).ViaTeams.Should().Equal("Accounting");
        extra.People.Single(p => p.UserId == minh).ViaTeams.Should().BeEmpty();
        extra.People.Single(p => p.UserId == stack.OwnerId).Creator.Should().BeTrue();
        extra.People.Count(p => p.Creator).Should().Be(1);
        extra.People.Should().OnlyContain(p => p.Email == null && p.LastActiveAt == null && !p.ActiveNow);
        extra.Guests.Should().BeEmpty();
    }

    [Fact]
    public async Task PendingInvitationNamingTheSpace_IsListedAndCounted_UntilItIsRevoked()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var space = await _host.SeedSpaceAsync(stack, "Finance", SpaceVisibility.Workspace, stack.OwnerId, type: SpaceType.Department);
        var other = await _host.SeedSpaceAsync(stack, "Legal", SpaceVisibility.Workspace, stack.OwnerId, type: SpaceType.Department);
        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);

        var invitedId = await SeedInvitationAsync(stack, "new.person@example.com", new WorkspaceInvitationInitialEntry(WorkspaceInvitationEntryKind.Space, space, "Viewer"));
        await SeedInvitationAsync(stack, "elsewhere@example.com", new WorkspaceInvitationInitialEntry(WorkspaceInvitationEntryKind.Space, other, "Member"));
        await SeedInvitationAsync(stack, "plain@example.com");

        var extra = await OrgTreeTestHost.QueryAsync(asOwner, new GetSpaceMembersExtraQuery(stack.WorkspaceId, space));
        extra.Summary.Invites.Should().Be(1);
        var invitation = extra.Invitations.Should().ContainSingle().Subject;
        invitation.Id.Should().Be(invitedId);
        invitation.Email.Should().Be("new.person@example.com");
        invitation.Role.Should().Be("Viewer");

        await _host.WithSystemContextAsync(async c =>
        {
            c.WorkspaceInvitations.Single(i => i.Id == invitedId).Revoke(stack.OwnerId, DateTimeOffset.UtcNow.AddMinutes(1));
            await c.SaveChangesAsync();
        });
        var after = await OrgTreeTestHost.QueryAsync(asOwner, new GetSpaceMembersExtraQuery(stack.WorkspaceId, space));
        after.Invitations.Should().BeEmpty("a revoked invitation is no longer pending");
        after.Summary.Invites.Should().Be(0);
    }

    [Fact]
    public async Task APrivateSpaceTheCallerCannotSee_IsNotFound_AndGuestOutsiderAndOtherWorkspaceAreRefused()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var other = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var outsider = await _host.AddOutsiderAsync(stack);
        var hidden = await _host.SeedSpaceAsync(stack, "Board of directors", SpaceVisibility.Private, stack.OwnerId);
        var foreign = await _host.SeedSpaceAsync(other, "Foreign", SpaceVisibility.Workspace, other.OwnerId);
        using var asMember = _host.CreateProvider(stack.AccountId, member);
        using var asOwner = _host.CreateProvider(stack.AccountId, stack.OwnerId);

        var hiddenRead = () => OrgTreeTestHost.QueryAsync(asMember, new GetSpaceMembersExtraQuery(stack.WorkspaceId, hidden));
        await hiddenRead.Should().ThrowAsync<AppNotFound>("a Private Space the member cannot see answers like a missing one");
        (await OrgTreeTestHost.QueryAsync(asOwner, new GetSpaceMembersExtraQuery(stack.WorkspaceId, hidden))).People.Should().ContainSingle();

        foreach (var userId in new[] { guest, outsider })
        {
            using var provider = _host.CreateProvider(stack.AccountId, userId);
            var read = () => OrgTreeTestHost.QueryAsync(provider, new GetSpaceMembersExtraQuery(stack.WorkspaceId, hidden));
            await read.Should().ThrowAsync<AppForbidden>();
        }

        var cross = () => OrgTreeTestHost.QueryAsync(asOwner, new GetSpaceMembersExtraQuery(stack.WorkspaceId, foreign));
        await cross.Should().ThrowAsync<AppNotFound>("a Space of another Workspace is unreachable");
    }

    private async Task<Guid> SeedInvitationAsync(OrgStack stack, string email, params WorkspaceInvitationInitialEntry[] entries)
    {
        var invitation = WorkspaceInvitation.Create(
            stack.AccountId, stack.WorkspaceId, email, WorkspaceRole.Member,
            InvitationTokenHash.Create(Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N")), 1,
            stack.OwnerId, DateTimeOffset.UtcNow, TimeSpan.FromDays(7), entries);
        await _host.WithSystemContextAsync(c => { c.WorkspaceInvitations.Add(invitation); return Task.CompletedTask; });
        return invitation.Id;
    }
}
