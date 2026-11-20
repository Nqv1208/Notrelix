using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Requests;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Security.Tokens;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Rls;
using Notrelix.Testing.Application.Fakes;
using Npgsql;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// Accepting an invitation addressed to someone who already has a membership, over the real host as the
/// application role. Lead ruling 2026-11-20: acceptance never changes the role of an existing Active membership,
/// in either direction (role changes go through ChangeRole only); the accept still succeeds. The invitation's
/// Team and Space entries are applied only where the CURRENT role may hold them: a Guest is not a Team member
/// (<c>Workspaces_Team_GuestCannotJoin</c>) and holds only resource-level entries, so an existing Guest gets none of
/// them, and the skipped entries are recorded in the audit trail. A Suspended member is never admitted by an
/// invitation, and a Removed member returns only through a plain invitation (rules unchanged).
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class AcceptInvitationExistingMemberAppRoleTests : IAsyncLifetime
{
    private const string SkippedAction = "AcceptInvitationEntriesSkipped";

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private Guid _teamId;
    private Guid _spaceId;

    public AcceptInvitationExistingMemberAppRoleTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-accept-existing@example.com", "Aaa", "hash");

        var now = DateTimeOffset.UtcNow;
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        var workspace = await ctx.Workspaces.SingleAsync(w => w.Id == _a.WorkspaceId);
        workspace.UpdateSettings(WorkspaceSettings.Create(allowGuestInvites: true), _a.User.Id, now);
        var team = Team.Create(_a.AccountId, _a.WorkspaceId, "Design", _a.User.Id, now);
        var space = Space.Create(_a.AccountId, _a.WorkspaceId, "Client space", SpaceVisibility.Workspace, _a.User.Id, now);
        ctx.Teams.Add(team);
        ctx.Spaces.Add(space);
        await ctx.SaveChangesAsync();
        _teamId = team.Id;
        _spaceId = space.Id;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private Task<User> PersonAsync(WorkspaceRole role, WorkspaceMemberStatus status = WorkspaceMemberStatus.Active) =>
        AppRoleMembers.AddAsync(_fixture, _a, "Person", role, status, "hash");

    private WorkspaceInvitationInitialEntry[] Entries() =>
    [
        new(WorkspaceInvitationEntryKind.Team, _teamId, "Member"),
        new(WorkspaceInvitationEntryKind.Space, _spaceId, "Viewer"),
    ];

    private async Task<string> InviteAsync(User person, WorkspaceRole role, WorkspaceInvitationInitialEntry[]? entries = null)
    {
        var issued = new OneTimeTokenService().Generate(TokenPurpose.WorkspaceInvitation);
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        // The domain refuses a Guest invitation that names a Team, so a legacy one is created as Member and
        // then flipped to Guest directly in the table, the way the invitation looked before that refusal.
        var legacyGuest = role == WorkspaceRole.Guest && entries is not null;
        await using (var ctx = _fixture.CreateOwnerContext(system))
        {
            ctx.WorkspaceInvitations.Add(WorkspaceInvitation.Create(
                _a.AccountId, _a.WorkspaceId, person.NormalizedEmail.ToLowerInvariant(), legacyGuest ? WorkspaceRole.Member : role,
                InvitationTokenHash.Create(issued.TokenHash), issued.HashVersion, _a.User.Id,
                DateTimeOffset.UtcNow.AddMinutes(-5), initialEntries: entries));
            await ctx.SaveChangesAsync();
        }

        if (legacyGuest)
            await ScalarAsync(
                "UPDATE workspace.workspace_invitations SET role = 'Guest' WHERE workspace_id = @w AND email = @e",
                ("w", _a.WorkspaceId), ("e", person.NormalizedEmail.ToLowerInvariant()));
        return issued.RawToken;
    }

    private async Task<HttpResponseMessage> AcceptAsync(User person, string token)
    {
        using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);
        using var scope = factory.Services.CreateScope();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(person, null);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", jwt);
        var response = await client.PostAsJsonAsync("/api/v1/invitations/accept", new { token });
        await response.Content.LoadIntoBufferAsync();
        return response;
    }

    private async Task<string> ScalarAsync(string sql, params (string, object)[] p)
    {
        await using var c = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await c.OpenAsync();
        await using var command = new NpgsqlCommand(sql, c);
        foreach (var (name, value) in p) command.Parameters.AddWithValue(name, value);
        return Convert.ToString(await command.ExecuteScalarAsync()) ?? "";
    }

    private Task<string> RoleAsync(User person) => ScalarAsync(
        "SELECT role FROM workspace.workspace_members WHERE workspace_id = @w AND user_id = @u", ("w", _a.WorkspaceId), ("u", person.Id));

    private Task<string> StatusAsync(User person) => ScalarAsync(
        "SELECT status FROM workspace.workspace_members WHERE workspace_id = @w AND user_id = @u", ("w", _a.WorkspaceId), ("u", person.Id));

    private Task<string> MembershipRowsAsync(User person) => ScalarAsync(
        "SELECT count(*) FROM workspace.workspace_members WHERE workspace_id = @w AND user_id = @u", ("w", _a.WorkspaceId), ("u", person.Id));

    private Task<string> GrantRolesAsync(User person) => ScalarAsync(
        "SELECT array_to_string(role_codes, ',') FROM authz.access_grants WHERE workspace_id = @w AND user_id = @u", ("w", _a.WorkspaceId), ("u", person.Id));

    private Task<string> TeamRowsAsync(User person) => ScalarAsync(
        "SELECT count(*) FROM workspace.team_members WHERE team_id = @t AND user_id = @u", ("t", _teamId), ("u", person.Id));

    private Task<string> SpaceRowsAsync(User person) => ScalarAsync(
        "SELECT count(*) FROM workspace.space_members WHERE space_id = @s AND subject_id = @u", ("s", _spaceId), ("u", person.Id));

    private Task<string> RoleChangeSignalsAsync(User person) => ScalarAsync(
        "SELECT (SELECT count(*) FROM messaging.outbox_messages WHERE account_id = @a AND message_name ILIKE '%role%changed%') " +
        "+ (SELECT count(*) FROM audit.audit_logs WHERE workspace_id = @w AND action ILIKE '%role%')",
        ("a", _a.AccountId), ("w", _a.WorkspaceId));

    private Task<string> SkippedAuditRowsAsync(User person) => ScalarAsync(
        "SELECT count(*) FROM audit.audit_logs WHERE workspace_id = @w AND action = @action AND subject_id = @u",
        ("w", _a.WorkspaceId), ("action", SkippedAction), ("u", person.Id));

    [Fact]
    public async Task AnExistingGuest_AcceptingAMemberInvitation_KeepsTheGuestRole_AndChangesNothingElse()
    {
        var guest = await PersonAsync(WorkspaceRole.Guest);
        var token = await InviteAsync(guest, WorkspaceRole.Member);
        var signalsBefore = await RoleChangeSignalsAsync(guest);

        var response = await AcceptAsync(guest, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await RoleAsync(guest)).Should().Be("Guest", "an invitation never promotes an existing membership");
        (await StatusAsync(guest)).Should().Be("Active");
        (await MembershipRowsAsync(guest)).Should().Be("1");
        (await GrantRolesAsync(guest)).Should().Be("Guest", "the grant projection is not rewritten, so the guest seat and the access stay as they were");
        (await RoleChangeSignalsAsync(guest)).Should().Be(signalsBefore, "no role-changed event or audit row is written");
    }

    [Fact]
    public async Task AnExistingMember_AcceptingAGuestInvitation_KeepsTheMemberRole_AndChangesNothingElse()
    {
        var member = await PersonAsync(WorkspaceRole.Member);
        var token = await InviteAsync(member, WorkspaceRole.Guest);
        var signalsBefore = await RoleChangeSignalsAsync(member);

        var response = await AcceptAsync(member, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await RoleAsync(member)).Should().Be("Member", "an invitation never demotes an existing membership");
        (await StatusAsync(member)).Should().Be("Active");
        (await MembershipRowsAsync(member)).Should().Be("1");
        (await GrantRolesAsync(member)).Should().Be("Member");
        (await RoleChangeSignalsAsync(member)).Should().Be(signalsBefore);
    }

    [Theory]
    [InlineData(WorkspaceRole.Member)]
    [InlineData(WorkspaceRole.Guest)]
    public async Task AnExistingGuest_AcceptingAnInvitationWithEntries_GetsNoTeamOrSpaceEntry_AndTheSkippedEntriesAreAudited(WorkspaceRole invited)
    {
        var guest = await PersonAsync(WorkspaceRole.Guest);
        var token = await InviteAsync(guest, invited, Entries());

        var response = await AcceptAsync(guest, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await RoleAsync(guest)).Should().Be("Guest");
        (await TeamRowsAsync(guest)).Should().Be("0", "a Guest is not a Team member");
        (await SpaceRowsAsync(guest)).Should().Be("0", "a Guest holds only resource-level entries");
        (await SkippedAuditRowsAsync(guest)).Should().Be("1", "the skipped entries are reported in the audit trail");
    }

    [Fact]
    public async Task AnExistingMember_AcceptingAnInvitationWithEntries_GetsThem_AndARepeatChangesNothing()
    {
        var member = await PersonAsync(WorkspaceRole.Member);
        var token = await InviteAsync(member, WorkspaceRole.Member, Entries());

        var first = await AcceptAsync(member, token);

        first.StatusCode.Should().Be(HttpStatusCode.OK, await first.Content.ReadAsStringAsync());
        (await TeamRowsAsync(member)).Should().Be("1");
        (await SpaceRowsAsync(member)).Should().Be("1");
        (await SkippedAuditRowsAsync(member)).Should().Be("0", "nothing was skipped for a Member");

        var repeat = await AcceptAsync(member, token);

        ((int)repeat.StatusCode).Should().BeLessThan(500, await repeat.Content.ReadAsStringAsync());
        (await RoleAsync(member)).Should().Be("Member");
        (await MembershipRowsAsync(member)).Should().Be("1");
        (await TeamRowsAsync(member)).Should().Be("1");
        (await SpaceRowsAsync(member)).Should().Be("1");
    }

    [Fact]
    public async Task ASuspendedMember_AcceptingAnInvitation_IsRefused_AndStaysSuspended()
    {
        var suspended = await PersonAsync(WorkspaceRole.Member, WorkspaceMemberStatus.Suspended);
        var token = await InviteAsync(suspended, WorkspaceRole.Member, Entries());

        var response = await AcceptAsync(suspended, token);

        response.IsSuccessStatusCode.Should().BeFalse(await response.Content.ReadAsStringAsync());
        (await StatusAsync(suspended)).Should().Be("Suspended");
        (await RoleAsync(suspended)).Should().Be("Member");
        (await TeamRowsAsync(suspended)).Should().Be("0");
        (await SpaceRowsAsync(suspended)).Should().Be("0");
    }

    [Fact]
    public async Task ARemovedMember_ReturnsThroughAPlainInvitation_WithTheInvitedRole()
    {
        var removed = await PersonAsync(WorkspaceRole.Member, WorkspaceMemberStatus.Removed);
        var token = await InviteAsync(removed, WorkspaceRole.Guest);

        var response = await AcceptAsync(removed, token);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await StatusAsync(removed)).Should().Be("Active");
        (await RoleAsync(removed)).Should().Be("Guest");
        (await MembershipRowsAsync(removed)).Should().Be("1");
    }

    [Fact]
    public async Task ARemovedMember_IsRefusedByAnInvitationThatCarriesEntries_AndStaysRemoved()
    {
        var removed = await PersonAsync(WorkspaceRole.Member, WorkspaceMemberStatus.Removed);
        var token = await InviteAsync(removed, WorkspaceRole.Member, Entries());

        var response = await AcceptAsync(removed, token);

        response.IsSuccessStatusCode.Should().BeFalse(await response.Content.ReadAsStringAsync());
        (await StatusAsync(removed)).Should().Be("Removed");
        (await TeamRowsAsync(removed)).Should().Be("0");
        (await SpaceRowsAsync(removed)).Should().Be("0");
    }
}
