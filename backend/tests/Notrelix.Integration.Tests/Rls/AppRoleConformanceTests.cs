using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Application.Common.Requests;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Domain.Workspaces.Invitations;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Security.ApiTokens;
using Notrelix.Infrastructure.Security.Tokens;
using Npgsql;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Conformance harness and break inventory for the application database role. The host under test
/// connects as a non-owner login running as <c>notrelix_app</c> with the policy pack applied; data is
/// seeded as the owner. Every flow is first proven to work against the owner connection (control), then
/// asserted against the application role. Flows that are broken under the application role today assert
/// that broken behaviour explicitly, under a name that says so: when a later change fixes a flow, its
/// test fails and is flipped to assert the working behaviour. Nothing here is skipped.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class AppRoleConformanceTests : IAsyncLifetime
{
    private const string Password = "Correct-Horse-9!";
    private readonly AppRoleTestContainer _fixture;
    private User _alice = null!;
    private Guid _workspaceId;
    private Guid _boardId;
    private string _apiTokenSecret = string.Empty;
    private string _invitationToken = string.Empty;

    public AppRoleConformanceTests(AppRoleTestContainer fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        var now = DateTimeOffset.UtcNow;
        string hash;
        using (var f = CreateFactory(_fixture.OwnerConnectionString))
        using (var scope = f.Services.CreateScope())
            hash = scope.ServiceProvider.GetRequiredService<IPasswordHasher>().HashPassword(Password);

        _alice = User.Create("alice-rls@example.com", "Alice", hash, now, hasPasswordCredential: true);
        _alice.ConfirmEmail(_alice.Id, now);
        var account = Account.Create("Acme", $"acme-{Guid.NewGuid():N}"[..20], AccountType.Team, _alice.Id, now);
        var ws = Workspace.Create(account.Id, _alice.Id, "WS", $"ws-{Guid.NewGuid():N}"[..16], now);
        _workspaceId = ws.Id;
        var board = Board.Create(account.Id, ws.Id, _alice.Id, "Roadmap", null, now);
        _boardId = board.Id;

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(tenant);
        ctx.Users.Add(_alice);
        ctx.Accounts.Add(account);
        ctx.Workspaces.Add(ws);
        ctx.Boards.Add(board);
        ctx.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, ws.Id, _alice.Id, WorkspaceRole.Owner, _alice.Id, now));
        ctx.AccessGrants.Add(new AccessGrant(
            accountId: account.Id, workspaceId: ws.Id, userId: _alice.Id, sourceContext: "Workspace",
            membershipStatus: "Active", roleCodes: ["Owner"], permissionCodes: [], isAccountAdmin: false,
            isWorkspaceAdmin: true, grantedAt: now));
        var secret = new ApiTokenSecretService().Generate();
        _apiTokenSecret = secret.RawToken;
        ctx.ApiTokens.Add(ApiToken.Create(account.Id, ws.Id, _alice.Id, "conformance", secret.TokenHash,
            scopes: null, createdBy: _alice.Id, createdAt: now, expiresAt: null));
        var issued = new OneTimeTokenService().Generate(TokenPurpose.WorkspaceInvitation);
        _invitationToken = issued.RawToken;
        ctx.WorkspaceInvitations.Add(WorkspaceInvitation.Create(
            account.Id, ws.Id, "invitee-rls@example.com", WorkspaceRole.Member,
            InvitationTokenHash.Create(issued.TokenHash), issued.HashVersion, _alice.Id, now.AddMinutes(-5)));
        await ctx.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private WebApplicationFactory<Program> CreateFactory(string db) =>
        AppRoleHost.CreateFactory(_fixture, db);

    private string Jwt()
    {
        using var f = CreateFactory(_fixture.OwnerConnectionString);
        using var scope = f.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<Notrelix.Application.Features.Identity.Auth.Sessions.IJwtService>().GenerateAccessToken(_alice);
    }

    private async Task<HttpResponseMessage> CallAsync(
        string db, Func<HttpClient, Task<HttpResponseMessage>> call, bool jwt = false, string? bearer = null)
    {
        using var factory = CreateFactory(db);
        using var client = factory.CreateClient();
        if (jwt) client.DefaultRequestHeaders.Authorization = new("Bearer", Jwt());
        if (bearer is not null) client.DefaultRequestHeaders.Authorization = new("Bearer", bearer);
        var response = await call(client);
        await response.Content.LoadIntoBufferAsync();
        return response;
    }

    private async Task<(HttpResponseMessage Owner, HttpResponseMessage App)> BothAsync(
        Func<HttpClient, Task<HttpResponseMessage>> call, bool jwt = false, string? bearer = null) =>
        (await CallAsync(_fixture.OwnerConnectionString, call, jwt, bearer),
         await CallAsync(_fixture.AppConnectionString, call, jwt, bearer));

    private async Task<string> ScalarAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        return Convert.ToString(await command.ExecuteScalarAsync()) ?? "";
    }

    [Fact]
    public async Task AppRoleLogin_IsNotPrivileged_SoThePoliciesApply()
    {
        (await ScalarAsync("SELECT current_user::text")).Should().Be("notrelix_app");
        (await ScalarAsync("SELECT rolsuper OR rolbypassrls FROM pg_roles WHERE rolname = current_user")).Should().Be("False");
        (await ScalarAsync("SELECT count(*) FROM identity.users")).Should().Be("0", "no session context means no visible rows");
    }

    [Fact]
    public async Task Register_UnderTheAppRole_Works()
    {
        var owner = await CallAsync(_fixture.OwnerConnectionString, c => c.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "new-owner-rls@example.com", password = Password, name = "NewOwner" }));
        owner.IsSuccessStatusCode.Should().BeTrue(await owner.Content.ReadAsStringAsync());

        var app = await CallAsync(_fixture.AppConnectionString, c => c.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "new-app-rls@example.com", password = Password, name = "NewApp" }));
        app.StatusCode.Should().Be(owner.StatusCode, await app.Content.ReadAsStringAsync());

        (await ScalarOwnerAsync(
            "SELECT count(*) FROM authz.access_grants g JOIN identity.users u ON u.id = g.user_id " +
            "WHERE u.normalized_email = 'new-app-rls@example.com' AND g.workspace_id IS NULL AND g.is_account_admin"))
            .Should().Be("1", "the new account's owner grant was written through the bounded function");
        (await ScalarOwnerAsync(
            "SELECT count(*) FROM account.accounts a JOIN account.account_members m ON m.account_id = a.id " +
            "JOIN identity.users u ON u.id = m.user_id WHERE u.normalized_email = 'new-app-rls@example.com'"))
            .Should().Be("1");

        var duplicate = await CallAsync(_fixture.AppConnectionString, c => c.PostAsJsonAsync("/api/v1/auth/register",
            new { email = "new-app-rls@example.com", password = Password, name = "Again" }));
        duplicate.StatusCode.Should().Be(HttpStatusCode.BadRequest, "an existing address is reported through the boolean probe, not a row read");
    }

    private async Task<string> ScalarOwnerAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return Convert.ToString(await command.ExecuteScalarAsync()) ?? "";
    }

    [Fact]
    public async Task Login_UnderTheAppRole_CurrentlyRejectsAKnownUserWith401()
    {
        var (owner, app) = await BothAsync(c => c.PostAsJsonAsync("/api/v1/auth/login",
            new { email = "alice-rls@example.com", password = Password }));
        owner.StatusCode.Should().Be(HttpStatusCode.OK);
        app.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "the user row is invisible without a session context");
    }

    [Fact]
    public async Task InvitationPreview_UnderTheAppRole_Works()
    {
        var (owner, app) = await BothAsync(c => c.PostAsJsonAsync("/api/v1/invitations/preview", new { token = _invitationToken }));
        owner.StatusCode.Should().Be(HttpStatusCode.OK);
        app.StatusCode.Should().Be(HttpStatusCode.OK, await app.Content.ReadAsStringAsync());
        var body = await app.Content.ReadAsStringAsync();
        body.Should().Contain("\"valid\"").And.Contain("Alice", "the inviter's display name resolves for the token holder").And.Contain("WS");
    }

    [Theory]
    [InlineData("/api/v1/auth/me", "alice-rls@example.com")]
    [InlineData("/api/v1/auth/bootstrap", "Alice")]
    [InlineData("/api/v1/account/workspaces", "WS")]
    [InlineData("/api/v1/profile/preferences", "")]
    [InlineData("/api/v1/auth/mfa/status", "")]
    [InlineData("/api/v1/auth/sessions", "")]
    public async Task GlobalAuthenticatedRequest_UnderTheAppRole_Works(string path, string expectedBodyFragment)
    {
        var (owner, app) = await BothAsync(c => c.GetAsync(path), jwt: true);
        owner.StatusCode.Should().Be(HttpStatusCode.OK, await owner.Content.ReadAsStringAsync());
        app.StatusCode.Should().Be(HttpStatusCode.OK, await app.Content.ReadAsStringAsync());
        if (expectedBodyFragment.Length > 0)
            (await app.Content.ReadAsStringAsync()).Should().Contain(expectedBodyFragment);
    }

    [Fact]
    public async Task ProfileUpdate_UnderTheAppRole_PersistsTheCallersOwnRow()
    {
        var app = await CallAsync(_fixture.AppConnectionString,
            c => c.PatchAsJsonAsync("/api/v1/profile/", new { name = "Alice Renamed" }), jwt: true);
        app.StatusCode.Should().Be(HttpStatusCode.OK, await app.Content.ReadAsStringAsync());

        var me = await CallAsync(_fixture.AppConnectionString, c => c.GetAsync("/api/v1/auth/me"), jwt: true);
        (await me.Content.ReadAsStringAsync()).Should().Contain("Alice Renamed");
    }

    [Fact]
    public async Task WorkspaceScopedRequest_UnderTheAppRole_Works()
    {
        var (owner, app) = await BothAsync(c => c.GetAsync($"/api/v1/workspaces/{_workspaceId}"), jwt: true);
        owner.StatusCode.Should().Be(HttpStatusCode.OK, await owner.Content.ReadAsStringAsync());
        app.StatusCode.Should().Be(HttpStatusCode.OK, "the workspace bootstrap sets a minimal context before the request data session");
    }

    [Fact]
    public async Task ResourceScopedRequest_UnderTheAppRole_Works()
    {
        var (owner, app) = await BothAsync(c => c.GetAsync($"/api/v1/boards/{_boardId}"), jwt: true);
        owner.StatusCode.Should().Be(HttpStatusCode.OK, await owner.Content.ReadAsStringAsync());
        app.StatusCode.Should().Be(HttpStatusCode.OK, "the resource locator runs in the user scope before the tenant is known");
    }

    [Fact]
    public async Task ApiTokenAuthentication_UnderTheAppRole_CurrentlyRejectsAValidToken()
    {
        var url = $"/api/v1/workspaces/{_workspaceId}/api-tokens";
        var (owner, app) = await BothAsync(c => c.GetAsync(url), bearer: _apiTokenSecret);
        owner.StatusCode.Should().Be(HttpStatusCode.OK, await owner.Content.ReadAsStringAsync());
        app.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("INSERT INTO account.accounts (id) VALUES (gen_random_uuid())")]
    [InlineData("INSERT INTO authz.access_grants (id) VALUES (gen_random_uuid())")]
    public async Task BootstrapInserts_UnderTheAppRole_AreCurrentlyDeniedByPolicy(string sql)
    {
        var act = () => ScalarAsync(sql);
        (await act.Should().ThrowAsync<PostgresException>()).Which.MessageText.Should().Contain("row-level security");
    }

    [Theory]
    [InlineData("messaging", "outbox_messages")]
    [InlineData("messaging", "processed_events")]
    [InlineData("notifications", "email_outbox")]
    [InlineData("ops", "job_locks")]
    public async Task WorkerInternalTables_HaveNoReadPolicyForTheAppRole_SoDispatchersAreBlindUnderIt(string schema, string table)
    {
        // The dispatchers, dedup filter and job locks read these tables through the application connection.
        var visible = await ScalarAsync(
            "SELECT count(*) FROM pg_policies WHERE schemaname = @s AND tablename = @t " +
            "AND cmd IN ('SELECT','ALL') AND (roles @> ARRAY['notrelix_app']::name[] OR roles @> ARRAY['public']::name[])",
            ("s", schema), ("t", table));
        visible.Should().Be("0");
    }

    [Fact]
    public async Task CoMemberProfile_UnderTheAppRole_ResolvesThroughTheFunction_WhileTheUsersRowStaysHidden()
    {
        var bob = User.Create("bob-rls@example.com", "Bob", "hash", DateTimeOffset.UtcNow, hasPasswordCredential: true);
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using (var ctx = _fixture.CreateOwnerContext(tenant))
        {
            ctx.Users.Add(bob);
            ctx.WorkspaceMembers.Add(WorkspaceMember.Create(
                (await ctx.Workspaces.IgnoreQueryFilters().SingleAsync(w => w.Id == _workspaceId)).AccountId,
                _workspaceId, bob.Id, WorkspaceRole.Member, _alice.Id, DateTimeOffset.UtcNow));
            await ctx.SaveChangesAsync();
        }

        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var tx = await connection.BeginTransactionAsync();
        await using (var set = new NpgsqlCommand("SELECT set_config('app.current_user_id', @u, true), set_config('app.request_scope', 'app', true)", connection, tx))
        {
            set.Parameters.AddWithValue("u", bob.Id.ToString());
            await set.ExecuteNonQueryAsync();
        }

        await using (var row = new NpgsqlCommand("SELECT count(*) FROM identity.users WHERE id = @id", connection, tx))
        {
            row.Parameters.AddWithValue("id", _alice.Id);
            Convert.ToInt64(await row.ExecuteScalarAsync()).Should().Be(0, "the row policy still exposes only the caller's own row");
        }

        await using var resolve = new NpgsqlCommand("SELECT name FROM ops.co_member_profiles(@ids)", connection, tx);
        resolve.Parameters.AddWithValue("ids", new[] { _alice.Id });
        (await resolve.ExecuteScalarAsync()).Should().Be("Alice", "a co-member's display name resolves through the owner-defined function");
    }
}
