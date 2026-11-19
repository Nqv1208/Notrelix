using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Requests;
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
/// A Guest invitation that named a Team entry before the invite-time refusal existed (one already in flight) is accepted
/// over the real host as the application role: the new Guest joins as a Guest, is NOT made a Team member (a Guest cannot
/// join a Team, <c>Workspaces_Team_GuestCannotJoin</c>), still gets the Space entry (a Guest may hold one, GOV-030 caps
/// its level), and the skipped Team entry is recorded in the audit trail.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class AcceptInFlightGuestInvitationAppRoleTests : IAsyncLifetime
{
    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private Guid _teamId;
    private Guid _spaceId;

    public AcceptInFlightGuestInvitationAppRoleTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-inflight-guest@example.com", "Aaa", "hash");
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

    private async Task<string> ScalarAsync(string sql, params (string, object)[] p)
    {
        await using var c = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await c.OpenAsync();
        await using var command = new NpgsqlCommand(sql, c);
        foreach (var (name, value) in p) command.Parameters.AddWithValue(name, value);
        return Convert.ToString(await command.ExecuteScalarAsync()) ?? "";
    }

    [Fact]
    public async Task ANewGuest_AcceptingAnInFlightInvitationWithATeamEntry_IsNotMadeATeamMember_AndTheSkippedEntryIsAudited()
    {
        var person = await AppRoleUsers.AddAsync(_fixture, $"inflight-{Guid.NewGuid():N}@example.com", "Person", "hash");
        var issued = new OneTimeTokenService().Generate(TokenPurpose.WorkspaceInvitation);
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using (var ctx = _fixture.CreateOwnerContext(system))
        {
            ctx.WorkspaceInvitations.Add(WorkspaceInvitation.Create(
                _a.AccountId, _a.WorkspaceId, person.NormalizedEmail.ToLowerInvariant(), WorkspaceRole.Member,
                InvitationTokenHash.Create(issued.TokenHash), issued.HashVersion, _a.User.Id, DateTimeOffset.UtcNow.AddMinutes(-5),
                initialEntries:
                [
                    new(WorkspaceInvitationEntryKind.Team, _teamId, "Member"),
                    new(WorkspaceInvitationEntryKind.Space, _spaceId, "Viewer"),
                ]));
            await ctx.SaveChangesAsync();
        }

        // The domain no longer creates a Guest invitation that names a Team; this one predates that refusal.
        await ScalarAsync(
            "UPDATE workspace.workspace_invitations SET role = 'Guest' WHERE workspace_id = @w AND email = @e",
            ("w", _a.WorkspaceId), ("e", person.NormalizedEmail.ToLowerInvariant()));

        using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);
        using var scope = factory.Services.CreateScope();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(person, null);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", jwt);

        var response = await client.PostAsJsonAsync("/api/v1/invitations/accept", new { token = issued.RawToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await ScalarAsync("SELECT role FROM workspace.workspace_members WHERE workspace_id = @w AND user_id = @u",
            ("w", _a.WorkspaceId), ("u", person.Id))).Should().Be("Guest");
        (await ScalarAsync("SELECT count(*) FROM workspace.team_members WHERE team_id = @t AND user_id = @u",
            ("t", _teamId), ("u", person.Id))).Should().Be("0", "a Guest is not a Team member");
        (await ScalarAsync("SELECT count(*) FROM workspace.space_members WHERE space_id = @s AND subject_id = @u",
            ("s", _spaceId), ("u", person.Id))).Should().Be("1", "a Guest may hold a Space entry");
        (await ScalarAsync(
            "SELECT count(*) FROM audit.audit_logs WHERE workspace_id = @w AND action = 'AcceptInvitationEntriesSkipped' AND subject_id = @u",
            ("w", _a.WorkspaceId), ("u", person.Id))).Should().Be("1", "the skipped Team entry is reported in the audit trail");
    }
}
