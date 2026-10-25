using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Integration.Tests.Containers;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// The invitation flows (preview, decline, accept while signed in, register-and-accept) under the
/// application role, and the bounds of the token, invitee and registration scopes they use: a presented
/// token reaches only its own invitation, an unknown, expired or used token enters nothing useful, the
/// grant write function refuses everything outside its scope, and every scope ends with its transaction.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class InvitationScopeTests : IAsyncLifetime
{
    private const string Password = "Correct-Horse-9!";
    private readonly AppRoleTestContainer _fixture;
    private string _hash = string.Empty;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;

    public InvitationScopeTests(AppRoleTestContainer fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        using (var f = AppRoleHost.CreateFactory(_fixture, _fixture.OwnerConnectionString))
        using (var scope = f.Services.CreateScope())
            _hash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().HashPassword(Password);

        _a = await AppRoleTenants.SeedAsync(_fixture, "a-invite@example.com", "Aaa", _hash);
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-invite@example.com", "Bbb", _hash);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<HttpResponseMessage> CallAsync(Func<HttpClient, Task<HttpResponseMessage>> call, Domain.Identity.Users.User? jwtUser = null)
    {
        using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);
        using var client = factory.CreateClient();
        if (jwtUser is not null)
        {
            using var scope = factory.Services.CreateScope();
            client.DefaultRequestHeaders.Authorization = new("Bearer",
                scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(jwtUser));
        }

        var response = await call(client);
        await response.Content.LoadIntoBufferAsync();
        return response;
    }

    private async Task<string> OwnerScalarAsync(string sql, params (string, object)[] p)
    {
        await using var c = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await c.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, c);
        foreach (var (n, v) in p) cmd.Parameters.AddWithValue(n, v);
        return Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";
    }

    private Task<string> InvitationStatusAsync(AppRoleTenant t) =>
        OwnerScalarAsync("SELECT status FROM workspace.workspace_invitations WHERE token_hash = @h", ("h", t.InvitationTokenHash));

    private Task<string> GrantCountAsync(Guid userId) =>
        OwnerScalarAsync("SELECT count(*) FROM authz.access_grants WHERE user_id = @u", ("u", userId));

    // ───────────────────────── HTTP flows ─────────────────────────

    [Fact]
    public async Task Decline_ByToken_Works_AndTouchesOnlyThatInvitation()
    {
        var response = await CallAsync(c => c.PostAsJsonAsync("/api/v1/invitations/decline", new { token = _a.InvitationToken }));
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());

        (await InvitationStatusAsync(_a)).Should().Be("Declined");
        (await InvitationStatusAsync(_b)).Should().Be("Pending", "the token of tenant A cannot change tenant B's invitation");
    }

    [Fact]
    public async Task Accept_WhileSignedIn_WritesTheMembershipAndGrantsThroughTheBoundedScope()
    {
        var invitee = await AppRoleUsers.AddAsync(_fixture, _a.InvitationEmail, "Invitee", _hash);

        var response = await CallAsync(c => c.PostAsJsonAsync("/api/v1/invitations/accept", new { token = _a.InvitationToken }), invitee);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        (await InvitationStatusAsync(_a)).Should().Be("Accepted");
        (await OwnerScalarAsync(
            "SELECT count(*) FROM workspace.workspace_members WHERE user_id = @u AND workspace_id = @w AND status = 'Active'",
            ("u", invitee.Id), ("w", _a.WorkspaceId))).Should().Be("1");
        (await OwnerScalarAsync(
            "SELECT count(*) FROM authz.access_grants WHERE user_id = @u AND account_id = @a AND workspace_id = @w AND 'Member' = ANY(role_codes) AND NOT is_workspace_admin",
            ("u", invitee.Id), ("a", _a.AccountId), ("w", _a.WorkspaceId))).Should().Be("1");
        (await OwnerScalarAsync(
            "SELECT count(*) FROM authz.access_grants WHERE user_id = @u AND account_id = @a AND workspace_id IS NULL AND NOT is_account_admin",
            ("u", invitee.Id), ("a", _a.AccountId))).Should().Be("1", "the account-level Member grant");
        (await GrantCountAsync(invitee.Id)).Should().Be("2", "nothing in tenant B");
    }

    [Fact]
    public async Task Accept_ByAPersonTheInvitationIsNotAddressedTo_IsRefused_AndWritesNothing()
    {
        var stranger = await AppRoleUsers.AddAsync(_fixture, "stranger-invite@example.com", "Stranger", _hash);

        var response = await CallAsync(c => c.PostAsJsonAsync("/api/v1/invitations/accept", new { token = _a.InvitationToken }), stranger);
        response.IsSuccessStatusCode.Should().BeFalse();

        (await InvitationStatusAsync(_a)).Should().Be("Pending");
        (await GrantCountAsync(stranger.Id)).Should().Be("0");
        (await OwnerScalarAsync("SELECT count(*) FROM workspace.workspace_members WHERE user_id = @u", ("u", stranger.Id))).Should().Be("0");
    }

    [Fact]
    public async Task Accept_WithAnExpiredOrUsedOrUnknownToken_WritesNothing()
    {
        var invitee = await AppRoleUsers.AddAsync(_fixture, _a.InvitationEmail, "Invitee", _hash);
        await OwnerScalarAsync("UPDATE workspace.workspace_invitations SET expires_at = now() - interval '1 day' WHERE token_hash = @h", ("h", _a.InvitationTokenHash));

        (await CallAsync(c => c.PostAsJsonAsync("/api/v1/invitations/accept", new { token = _a.InvitationToken }), invitee))
            .IsSuccessStatusCode.Should().BeFalse("an expired invitation cannot be accepted");
        (await CallAsync(c => c.PostAsJsonAsync("/api/v1/invitations/accept", new { token = "not-a-token" }), invitee))
            .IsSuccessStatusCode.Should().BeFalse();
        (await CallAsync(c => c.PostAsJsonAsync("/api/v1/invitations/accept", new { token = _b.InvitationToken }), invitee))
            .IsSuccessStatusCode.Should().BeFalse("tenant B's invitation is addressed to someone else");

        await OwnerScalarAsync(
            "UPDATE workspace.workspace_invitations SET expires_at = now() + interval '1 day', status = 'Declined' WHERE token_hash = @h", ("h", _a.InvitationTokenHash));
        (await CallAsync(c => c.PostAsJsonAsync("/api/v1/invitations/accept", new { token = _a.InvitationToken }), invitee))
            .IsSuccessStatusCode.Should().BeFalse("a used invitation cannot be accepted");

        (await GrantCountAsync(invitee.Id)).Should().Be("0");
        (await OwnerScalarAsync("SELECT count(*) FROM workspace.workspace_members WHERE user_id = @u", ("u", invitee.Id))).Should().Be("0");
    }

    [Fact]
    public async Task RegisterAndAccept_CreatesTheIdentityTheMembershipAndTheGrants_InOneBoundedTransaction()
    {
        var response = await CallAsync(c => c.PostAsJsonAsync("/api/v1/invitations/accept-with-registration",
            new { token = _a.InvitationToken, name = "Newcomer", password = Password }));
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var userId = Guid.Parse(await OwnerScalarAsync(
            "SELECT id FROM identity.users WHERE normalized_email = @e", ("e", _a.InvitationEmail)));
        (await InvitationStatusAsync(_a)).Should().Be("Accepted");
        (await OwnerScalarAsync(
            "SELECT count(*) FROM workspace.workspace_members WHERE user_id = @u AND workspace_id = @w", ("u", userId), ("w", _a.WorkspaceId))).Should().Be("1");
        (await OwnerScalarAsync(
            "SELECT count(*) FROM authz.access_grants WHERE user_id = @u AND workspace_id = @w", ("u", userId), ("w", _a.WorkspaceId))).Should().Be("1");
        (await OwnerScalarAsync(
            "SELECT count(*) FROM authz.access_grants g WHERE g.user_id = @u AND g.workspace_id IS NULL AND g.is_account_admin", ("u", userId)))
            .Should().Be("1", "the newcomer's own personal account owner grant");
        (await OwnerScalarAsync("SELECT count(*) FROM identity.users WHERE normalized_email = @e", ("e", _b.InvitationEmail))).Should().Be("0");
    }

    [Fact]
    public async Task Preview_OfAnUnknownMalformedOrExpiredToken_RevealsNothingBeyondTheDocumentedStates()
    {
        (await CallAsync(c => c.PostAsJsonAsync("/api/v1/invitations/preview", new { token = "nope" }))).StatusCode
            .Should().Be(HttpStatusCode.NotFound);

        await OwnerScalarAsync("UPDATE workspace.workspace_invitations SET expires_at = now() - interval '1 day' WHERE token_hash = @h", ("h", _a.InvitationTokenHash));
        var expired = await CallAsync(c => c.PostAsJsonAsync("/api/v1/invitations/preview", new { token = _a.InvitationToken }));
        expired.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await expired.Content.ReadAsStringAsync();
        body.Should().Contain("expired").And.NotContain(_a.InvitationEmail, "an unusable link withholds the invited address");
    }

    // ───────────────────────── scope bounds (SQL) ─────────────────────────

    private async Task<(NpgsqlConnection Connection, NpgsqlTransaction Tx)> BeginAsync()
    {
        var c = new NpgsqlConnection(_fixture.AppConnectionString);
        await c.OpenAsync();
        return (c, await c.BeginTransactionAsync());
    }

    private static async Task<string> QAsync(NpgsqlConnection c, NpgsqlTransaction tx, string sql, params (string, object)[] p)
    {
        await using var cmd = new NpgsqlCommand(sql, c, tx);
        foreach (var (n, v) in p) cmd.Parameters.AddWithValue(n, v);
        return Convert.ToString(await cmd.ExecuteScalarAsync()) ?? "";
    }

    private static Task<string> EnterTokenAsync(NpgsqlConnection c, NpgsqlTransaction tx, AppRoleTenant t) =>
        QAsync(c, tx, "SELECT count(*) FROM ops.enter_token_scope('workspace-invitation', @h, @v)",
            ("h", t.InvitationTokenHash), ("v", t.InvitationHashVersion));

    [Fact]
    public async Task TokenScope_SeesAndChangesOnlyItsOwnInvitation_AndTheInvitedWorkspaceRow_NothingElse()
    {
        var (c, tx) = await BeginAsync();
        await using var _ = c;
        await using var __ = tx;
        (await EnterTokenAsync(c, tx, _a)).Should().Be("1");

        (await QAsync(c, tx, "SELECT count(*) FROM workspace.workspace_invitations")).Should().Be("1");
        (await QAsync(c, tx, "SELECT count(*) FROM workspace.workspace_invitations WHERE workspace_id = @w", ("w", _b.WorkspaceId))).Should().Be("0");
        (await QAsync(c, tx, "SELECT count(*) FROM workspace.workspaces")).Should().Be("1");
        (await QAsync(c, tx, "SELECT count(*) FROM workspace.workspaces WHERE id = @w", ("w", _b.WorkspaceId))).Should().Be("0");
        (await QAsync(c, tx, "UPDATE workspace.workspace_invitations SET status = 'Declined' WHERE workspace_id = @w; SELECT 1", ("w", _b.WorkspaceId)))
            .Should().Be("1");
        (await QAsync(c, tx, "SELECT count(*) FROM work.boards")).Should().Be("0", "a plain invitation names no resource");
        (await QAsync(c, tx, "SELECT count(*) FROM workspace.workspace_members")).Should().Be("0");
        (await QAsync(c, tx, "SELECT count(*) FROM authz.access_grants")).Should().Be("0");
        (await QAsync(c, tx, "SELECT count(*) FROM identity.users")).Should().Be("0");

        await tx.RollbackAsync();
        (await InvitationStatusAsync(_b)).Should().Be("Pending");
    }

    [Fact]
    public async Task TokenScope_ResolvesTheInvitersDisplayFactsOnly()
    {
        var member = await AppRoleMembers.AddAsync(_fixture, _a, "Member", Domain.Workspaces.Members.WorkspaceRole.Member, Domain.Workspaces.Members.WorkspaceMemberStatus.Active, _hash);
        var (c, tx) = await BeginAsync();
        await using var _ = c;
        await using var __ = tx;
        (await EnterTokenAsync(c, tx, _a)).Should().Be("1");

        (await QAsync(c, tx, "SELECT name FROM ops.co_member_profiles(@ids)", ("ids", new[] { _a.User.Id }))).Should().Be("Aaa", "the inviter of this invitation");
        (await QAsync(c, tx, "SELECT count(*) FROM ops.co_member_profiles(@ids)", ("ids", new[] { member.Id, _b.User.Id }))).Should().Be("0");
    }

    [Fact]
    public async Task TokenScope_ForAResourceInvitation_ReachesExactlyTheNamedBoard()
    {
        // A resource invitation names one board of the workspace.
        await OwnerScalarAsync(
            "UPDATE workspace.workspace_invitations SET role = 'Guest', resource_kind = 'work-management.board', resource_id = @b, resource_level = 'Viewer' WHERE token_hash = @h",
            ("b", _a.BoardId), ("h", _a.InvitationTokenHash));

        var (c, tx) = await BeginAsync();
        await using var _ = c;
        await using var __ = tx;
        (await EnterTokenAsync(c, tx, _a)).Should().Be("1");

        (await QAsync(c, tx, "SELECT count(*) FROM work.boards")).Should().Be("1");
        (await QAsync(c, tx, "SELECT count(*) FROM work.boards WHERE id = @b", ("b", _b.BoardId))).Should().Be("0");
    }

    [Fact]
    public async Task UnknownTokens_EnterNothing_AndLeaveNoScope()
    {
        var (c, tx) = await BeginAsync();
        await using var _ = c;
        await using var __ = tx;

        (await QAsync(c, tx, "SELECT count(*) FROM ops.enter_token_scope('workspace-invitation', 'unknown-digest', 1)")).Should().Be("0");
        (await QAsync(c, tx, "SELECT ops.current_request_scope()")).Should().Be("app");
        (await QAsync(c, tx, "SELECT count(*) FROM workspace.workspace_invitations")).Should().Be("0");
        (await QAsync(c, tx, "SELECT ops.enter_invitee_scope(@u)", ("u", _a.User.Id))).Should().Be("False", "no token scope, so no invitee");
    }

    [Fact]
    public async Task TheScope_EndsWithItsTransaction_ForTheNextTransactionOnTheSameConnection()
    {
        await using var c = new NpgsqlConnection(_fixture.AppConnectionString);
        await c.OpenAsync();
        await using (var tx = await c.BeginTransactionAsync())
        {
            (await EnterTokenAsync(c, tx, _a)).Should().Be("1");
            (await QAsync(c, tx, "SELECT count(*) FROM workspace.workspace_invitations")).Should().Be("1");
            await tx.CommitAsync();
        }

        await using var next = await c.BeginTransactionAsync();
        (await QAsync(c, next, "SELECT ops.current_request_scope()")).Should().Be("app");
        (await QAsync(c, next, "SELECT count(*) FROM workspace.workspace_invitations")).Should().Be("0");
    }

    [Fact]
    public async Task InviteeScope_BindsOnlyTheAddressedPerson_AndOnlyWhilePendingAndUnexpired()
    {
        var invitee = await AppRoleUsers.AddAsync(_fixture, _a.InvitationEmail, "Invitee", _hash);
        var stranger = await AppRoleUsers.AddAsync(_fixture, "stranger2-invite@example.com", "Stranger", _hash);

        async Task<string> BindAsync(Guid asUser, Guid? callerUser = null)
        {
            var (c, tx) = await BeginAsync();
            await using var _ = c;
            await using var __ = tx;
            await QAsync(c, tx, "SELECT set_config('app.current_user_id', @u, true)", ("u", (callerUser ?? asUser).ToString()));
            await EnterTokenAsync(c, tx, _a);
            return await QAsync(c, tx, "SELECT ops.enter_invitee_scope(@u)", ("u", asUser));
        }

        (await BindAsync(invitee.Id)).Should().Be("True");
        (await BindAsync(stranger.Id)).Should().Be("False", "the invitation is addressed to another address");
        (await BindAsync(invitee.Id, callerUser: stranger.Id)).Should().Be("False", "the bound person must be the authenticated caller");

        await OwnerScalarAsync("UPDATE workspace.workspace_invitations SET expires_at = now() - interval '1 day' WHERE token_hash = @h", ("h", _a.InvitationTokenHash));
        (await BindAsync(invitee.Id)).Should().Be("False", "an expired invitation binds nobody");
    }

    [Fact]
    public async Task InviteeScope_GivesProvisionalAccessToTheInvitedWorkspaceOnly()
    {
        var invitee = await AppRoleUsers.AddAsync(_fixture, _a.InvitationEmail, "Invitee", _hash);
        var (c, tx) = await BeginAsync();
        await using var _ = c;
        await using var __ = tx;
        await QAsync(c, tx, "SELECT set_config('app.current_user_id', @u, true)", ("u", invitee.Id.ToString()));
        await EnterTokenAsync(c, tx, _a);
        (await QAsync(c, tx, "SELECT ops.enter_invitee_scope(@u)", ("u", invitee.Id))).Should().Be("True");

        (await QAsync(c, tx, "SELECT ops.has_workspace_access(@a, @w)", ("a", _a.AccountId), ("w", _a.WorkspaceId))).Should().Be("True");
        (await QAsync(c, tx, "SELECT ops.has_account_access(@a)", ("a", _a.AccountId))).Should().Be("True");
        (await QAsync(c, tx, "SELECT ops.has_workspace_access(@a, @w)", ("a", _b.AccountId), ("w", _b.WorkspaceId))).Should().Be("False");
        (await QAsync(c, tx, "SELECT ops.has_account_access(@a)", ("a", _b.AccountId))).Should().Be("False");
        (await QAsync(c, tx, "SELECT ops.is_workspace_admin(@a, @w)", ("a", _a.AccountId), ("w", _a.WorkspaceId))).Should().Be("False", "an invitee is never an admin");
        (await QAsync(c, tx, "SELECT count(*) FROM work.boards")).Should().Be("1", "the invited workspace's boards follow the grant-derived policy");
        (await QAsync(c, tx, "SELECT count(*) FROM work.boards WHERE id = @b", ("b", _b.BoardId))).Should().Be("0");
    }

    // ───────────────────────── registration scope and the grant function ─────────────────────────

    [Fact]
    public async Task RegistrationScope_CannotBeEnteredForAnExistingUser_AndTheGrantFunctionRefusesEverythingOutsideItsScope()
    {
        var (c, tx) = await BeginAsync();
        await using var _ = c;
        await using var __ = tx;

        (await QAsync(c, tx, "SELECT ops.enter_registration_scope(@u)", ("u", _a.User.Id))).Should().Be("False");

        async Task<string> GrantAsync(Guid account, Guid? workspace, Guid user, string role)
        {
            await QAsync(c, tx, "SAVEPOINT g");
            try
            {
                await using var cmd = new NpgsqlCommand("SELECT ops.upsert_access_grant(@a, @w, @u, @r, now())", c, tx);
                cmd.Parameters.AddWithValue("a", account);
                cmd.Parameters.Add(new NpgsqlParameter("w", NpgsqlTypes.NpgsqlDbType.Uuid) { Value = (object?)workspace ?? DBNull.Value });
                cmd.Parameters.AddWithValue("u", user);
                cmd.Parameters.AddWithValue("r", role);
                await cmd.ExecuteScalarAsync();
                await QAsync(c, tx, "RELEASE SAVEPOINT g");
                return "ok";
            }
            catch (PostgresException e)
            {
                await QAsync(c, tx, "ROLLBACK TO SAVEPOINT g");
                return e.SqlState;
            }
        }

        var newUser = Guid.NewGuid();
        (await GrantAsync(Guid.NewGuid(), null, newUser, "Owner")).Should().Be("42501", "no scope, no grant write");

        (await QAsync(c, tx, "SELECT ops.enter_registration_scope(@u)", ("u", newUser))).Should().Be("True");
        (await GrantAsync(_a.AccountId, null, newUser, "Owner")).Should().Be("42501", "an account that already exists cannot be claimed");
        (await GrantAsync(Guid.NewGuid(), null, _a.User.Id, "Owner")).Should().Be("42501", "only the new user can be granted");
        (await GrantAsync(Guid.NewGuid(), _a.WorkspaceId, newUser, "Owner")).Should().Be("42501", "a registration writes no workspace grant");
        (await GrantAsync(Guid.NewGuid(), null, newUser, "Admin")).Should().Be("42501", "only the owner grant of the new account");

        var account = Guid.NewGuid();
        (await GrantAsync(account, null, newUser, "Owner")).Should().Be("ok");
        (await GrantAsync(Guid.NewGuid(), null, newUser, "Owner")).Should().Be("42501", "the scope is bound to its one account");
        (await QAsync(c, tx, "SELECT ops.has_account_access(@a)", ("a", account))).Should().Be("True");
        (await QAsync(c, tx, "SELECT ops.has_account_access(@a)", ("a", _a.AccountId))).Should().Be("False");
    }

    [Fact]
    public async Task RegistrationScope_InsertsOnlyTheNewIdentity_AndTheOneAccountItNamed()
    {
        var (c, tx) = await BeginAsync();
        await using var _ = c;
        await using var __ = tx;
        var newUser = Guid.NewGuid();
        var account = Guid.NewGuid();
        (await QAsync(c, tx, "SELECT ops.enter_registration_scope(@u)", ("u", newUser))).Should().Be("True");

        async Task<string> TryAsync(string sql)
        {
            await QAsync(c, tx, "SAVEPOINT s");
            try { await QAsync(c, tx, sql); await QAsync(c, tx, "RELEASE SAVEPOINT s"); return "ok"; }
            catch (PostgresException e) { await QAsync(c, tx, "ROLLBACK TO SAVEPOINT s"); return e.SqlState; }
        }

        string InsertUser(Guid id, string email) =>
            "INSERT INTO identity.users (id, email, normalized_email, name, password_hash, has_password_credential, status, email_confirmed, created_at, updated_at) " +
            $"VALUES ('{id}', '{email}', '{email}', 'N', 'h', true, 'Active', false, now(), now()); SELECT 1";

        (await TryAsync(InsertUser(Guid.NewGuid(), "someone-else@example.com"))).Should().Be("42501", "only the user the scope was entered for");
        (await TryAsync(InsertUser(newUser, "me@example.com"))).Should().Be("ok");
        (await TryAsync($"INSERT INTO account.accounts (id, name, slug, status, type, created_at, updated_at) VALUES ('{Guid.NewGuid()}', 'x', 'x-{Guid.NewGuid():N}', 'Active', 'Personal', now(), now()); SELECT 1"))
            .Should().Be("42501", "no account was named by a grant yet");
        (await QAsync(c, tx, "SELECT ops.upsert_access_grant(@a, NULL, @u, 'Owner', now())::text", ("a", account), ("u", newUser))).Should().NotBeNull();
        (await TryAsync($"INSERT INTO account.accounts (id, name, slug, status, type, created_at, updated_at) VALUES ('{account}', 'x', 'x-{Guid.NewGuid():N}', 'Active', 'Personal', now(), now()); SELECT 1"))
            .Should().Be("ok");
        (await TryAsync($"INSERT INTO account.accounts (id, name, slug, status, type, created_at, updated_at) VALUES ('{Guid.NewGuid()}', 'y', 'y-{Guid.NewGuid():N}', 'Active', 'Personal', now(), now()); SELECT 1"))
            .Should().Be("42501", "a registration creates exactly one account");

        (await QAsync(c, tx, "SELECT count(*) FROM identity.users WHERE id = @u", ("u", _a.User.Id))).Should().Be("0", "no other user row is visible");
    }

    [Fact]
    public async Task EmailRegistered_AnswersABooleanOnly_ForAnyAddress()
    {
        var (c, tx) = await BeginAsync();
        await using var _ = c;
        await using var __ = tx;
        (await QAsync(c, tx, "SELECT ops.email_registered(@e)", ("e", "a-invite@example.com"))).Should().Be("True");
        (await QAsync(c, tx, "SELECT ops.email_registered(@e)", ("e", "nobody@example.com"))).Should().Be("False");
        (await QAsync(c, tx, "SELECT count(*) FROM identity.users")).Should().Be("0", "the probe reads no row on the caller's behalf");
    }
}
