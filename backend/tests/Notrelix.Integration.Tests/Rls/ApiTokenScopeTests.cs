using System.Net;
using Notrelix.Application.Features.Identity.ApiTokens.Abstractions;
using Notrelix.Domain.Identity.Tokens;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Security.ApiTokens;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// API-token authentication under the application role: the presented token's digest is looked up by the
/// owner-defined token-scope function, which enters a scope bound to that one token's own account and
/// workspace (and enters nothing for an unknown, revoked, expired or user-less token). Inside the scope the
/// application role reads and updates that single token row and nothing else.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class ApiTokenScopeTests : IAsyncLifetime
{
    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private IssuedApiTokenSecret _tokenA = null!;
    private IssuedApiTokenSecret _tokenB = null!;
    private Guid _tokenAId;

    public ApiTokenScopeTests(AppRoleTestContainer fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-token@example.com", "Aaa", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-token@example.com", "Bbb", "hash");
        (_tokenA, _tokenAId) = await AddTokenAsync(_a, "token-a");
        (_tokenB, _) = await AddTokenAsync(_b, "token-b");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<(IssuedApiTokenSecret Secret, Guid Id)> AddTokenAsync(AppRoleTenant tenant, string name)
    {
        var secret = new ApiTokenSecretService().Generate();
        var token = ApiToken.Create(tenant.AccountId, tenant.WorkspaceId, tenant.User.Id, name, secret.TokenHash,
            scopes: null, createdBy: tenant.User.Id, createdAt: DateTimeOffset.UtcNow, expiresAt: null);
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.ApiTokens.Add(token);
        await ctx.SaveChangesAsync();
        return (secret, token.Id);
    }

    private async Task<string> OwnerScalarAsync(string sql, params (string, object)[] p)
    {
        await using var c = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await c.OpenAsync();
        await using var command = new NpgsqlCommand(sql, c);
        foreach (var (name, value) in p) command.Parameters.AddWithValue(name, value);
        return Convert.ToString(await command.ExecuteScalarAsync()) ?? "";
    }

    private async Task<HttpStatusCode> CallAsync(string url, string bearer)
    {
        using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", bearer);
        return (await client.GetAsync(url)).StatusCode;
    }

    private static async Task<string> ScalarAsync(NpgsqlConnection c, NpgsqlTransaction tx, string sql, params (string, object)[] p)
    {
        await using var command = new NpgsqlCommand(sql, c, tx);
        foreach (var (name, value) in p) command.Parameters.AddWithValue(name, value);
        return Convert.ToString(await command.ExecuteScalarAsync()) ?? "";
    }

    private async Task<(NpgsqlConnection, NpgsqlTransaction)> BeginAppAsync()
    {
        var c = new NpgsqlConnection(_fixture.AppConnectionString);
        await c.OpenAsync();
        return (c, await c.BeginTransactionAsync());
    }

    [Fact]
    public async Task AValidToken_Authenticates_AndItsUseIsRecorded()
    {
        (await CallAsync($"/api/v1/workspaces/{_a.WorkspaceId}/api-tokens", _tokenA.RawToken)).Should().Be(HttpStatusCode.OK);
        (await OwnerScalarAsync("SELECT last_used_at IS NOT NULL FROM identity.api_tokens WHERE id = @i", ("i", _tokenAId)))
            .Should().Be("True", "the authentication step recorded the use of the one token row it entered the scope for");
    }

    [Fact]
    public async Task AnUnknownToken_IsRejected()
    {
        var unknown = new ApiTokenSecretService().Generate().RawToken;
        (await CallAsync($"/api/v1/workspaces/{_a.WorkspaceId}/api-tokens", unknown)).Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ARevokedToken_IsRejected()
    {
        await OwnerScalarAsync("UPDATE identity.api_tokens SET status = 'Revoked', revoked_at = now() WHERE id = @i", ("i", _tokenAId));
        (await CallAsync($"/api/v1/workspaces/{_a.WorkspaceId}/api-tokens", _tokenA.RawToken)).Should().Be(HttpStatusCode.Unauthorized);
        (await OwnerScalarAsync("SELECT last_used_at IS NULL FROM identity.api_tokens WHERE id = @i", ("i", _tokenAId)))
            .Should().Be("True", "a revoked token's use is not recorded");
    }

    [Fact]
    public async Task AnExpiredToken_IsRejected()
    {
        await OwnerScalarAsync("UPDATE identity.api_tokens SET expires_at = now() - interval '1 minute' WHERE id = @i", ("i", _tokenAId));
        (await CallAsync($"/api/v1/workspaces/{_a.WorkspaceId}/api-tokens", _tokenA.RawToken)).Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ATokenOfOneWorkspace_CannotReachAnotherWorkspace()
    {
        var status = await CallAsync($"/api/v1/workspaces/{_b.WorkspaceId}/api-tokens", _tokenA.RawToken);
        status.Should().NotBe(HttpStatusCode.OK);
        status.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    // The issuing user belongs to a second workspace of the same account and may open its boards themselves;
    // the token is bound to the first workspace only, so a board of the second is as missing as a made-up id.
    [Fact]
    public async Task ATokenCannotOpenABoardOfAnotherWorkspaceItsIssuerBelongsTo()
    {
        var now = DateTimeOffset.UtcNow;
        var second = Workspace.Create(_a.AccountId, _a.User.Id, "Second", $"ws2-{Guid.NewGuid():N}"[..16], now);
        var board = Board.Create(_a.AccountId, second.Id, _a.User.Id, "Elsewhere", null, now);
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using (var ctx = _fixture.CreateOwnerContext(system))
        {
            ctx.Workspaces.Add(second);
            ctx.Boards.Add(board);
            ctx.WorkspaceMembers.Add(WorkspaceMember.Create(_a.AccountId, second.Id, _a.User.Id, WorkspaceRole.Owner, _a.User.Id, now));
            ctx.AccessGrants.Add(new AccessGrant(
                accountId: _a.AccountId, workspaceId: second.Id, userId: _a.User.Id, sourceContext: "Workspace",
                membershipStatus: "Active", roleCodes: ["Owner"], permissionCodes: [], isAccountAdmin: false,
                isWorkspaceAdmin: true, grantedAt: now));
            await ctx.SaveChangesAsync();
        }

        (await CallAsync($"/api/v1/boards/{_a.BoardId}", _tokenA.RawToken)).Should().Be(HttpStatusCode.OK);
        (await CallAsync($"/api/v1/boards/{board.Id}", _tokenA.RawToken)).Should().Be(HttpStatusCode.NotFound);
    }

    // An integration token is a workspace credential, never a person: every route that belongs to the person
    // (sessions, security, second factor, providers, profile, preferences, sign-out) refuses it, whatever the
    // issuing user may do themselves.
    [Theory]
    [InlineData("GET", "/api/v1/auth/me", null)]
    [InlineData("GET", "/api/v1/auth/sessions", null)]
    [InlineData("GET", "/api/v1/auth/security/summary", null)]
    [InlineData("GET", "/api/v1/auth/security/activity", null)]
    [InlineData("GET", "/api/v1/auth/mfa/status", null)]
    [InlineData("GET", "/api/v1/profile/preferences", null)]
    [InlineData("POST", "/api/v1/auth/sessions/revoke-others", "{}")]
    [InlineData("POST", "/api/v1/auth/security/step-up/requirement", "{\"purpose\":\"ChangePassword\"}")]
    [InlineData("POST", "/api/v1/auth/security/step-up/complete-password", "{\"purpose\":\"ChangePassword\",\"password\":\"Passw0rd!Passw0rd!\"}")]
    [InlineData("POST", "/api/v1/auth/mfa/disable", "{}")]
    [InlineData("POST", "/api/v1/auth/change-password", "{\"currentPassword\":\"Passw0rd!Passw0rd!\",\"newPassword\":\"Other-Passw0rd!2\"}")]
    [InlineData("POST", "/api/v1/auth/set-password", "{\"newPassword\":\"Other-Passw0rd!2\",\"stepUpToken\":\"proof\"}")]
    [InlineData("POST", "/api/v1/auth/oauth/google/unlink", "{\"stepUpToken\":\"proof\"}")]
    [InlineData("POST", "/api/v1/profile/email", "{\"email\":\"new-address@example.com\"}")]
    [InlineData("PATCH", "/api/v1/profile", "{\"name\":\"Someone\"}")]
    [InlineData("PATCH", "/api/v1/profile/preferences", "{\"expectedVersion\":0,\"locale\":\"vi\"}")]
    public async Task ATokenCannotReachAPersonLevelRoute(string method, string url, string? body)
    {
        using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = body is null ? null : new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new("Bearer", _tokenA.RawToken);

        var status = (await client.SendAsync(request)).StatusCode;

        status.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TheScopeFunction_EntersOnlyForAnActiveUnexpiredUserBoundToken()
    {
        const string enter = "SELECT count(*) FROM ops.enter_token_scope('api-token', @h, 0)";

        await OwnerScalarAsync("UPDATE identity.api_tokens SET status = 'Revoked' WHERE id = @i", ("i", _tokenAId));
        var (c, tx) = await BeginAppAsync();
        await using (c)
        await using (tx)
        {
            (await ScalarAsync(c, tx, enter, ("h", _tokenA.TokenHash))).Should().Be("0", "a revoked token enters nothing");
            (await ScalarAsync(c, tx, "SELECT ops.current_request_scope()")).Should().Be("app", "no scope was entered");
        }

        await OwnerScalarAsync("UPDATE identity.api_tokens SET status = 'Active', expires_at = now() - interval '1 second' WHERE id = @i", ("i", _tokenAId));
        (c, tx) = await BeginAppAsync();
        await using (c)
        await using (tx)
            (await ScalarAsync(c, tx, enter, ("h", _tokenA.TokenHash))).Should().Be("0", "an expired token enters nothing");

        await OwnerScalarAsync("UPDATE identity.api_tokens SET expires_at = NULL, user_id = NULL WHERE id = @i", ("i", _tokenAId));
        (c, tx) = await BeginAppAsync();
        await using (c)
        await using (tx)
            (await ScalarAsync(c, tx, enter, ("h", _tokenA.TokenHash))).Should().Be("0", "a user-less token enters nothing");

        (c, tx) = await BeginAppAsync();
        await using (c)
        await using (tx)
            (await ScalarAsync(c, tx, enter, ("h", "no-such-digest"))).Should().Be("0", "an unknown digest enters nothing");
    }

    [Fact]
    public async Task InsideTheTokenScope_TheApplicationRoleSeesAndUpdatesOnlyThatOneTokenRow()
    {
        var (c, tx) = await BeginAppAsync();
        await using var _ = c;
        await using var __ = tx;

        (await ScalarAsync(c, tx, "SELECT count(*) FROM identity.api_tokens")).Should().Be("0", "no scope, no token rows");
        (await ScalarAsync(c, tx, "SELECT account_id || '/' || workspace_id FROM ops.enter_token_scope('api-token', @h, 0)",
            ("h", _tokenA.TokenHash))).Should().Be($"{_a.AccountId}/{_a.WorkspaceId}");

        (await ScalarAsync(c, tx, "SELECT count(*) FROM identity.api_tokens")).Should().Be("1", "only the token's own row is visible");
        (await ScalarAsync(c, tx, "SELECT count(*) FROM identity.api_tokens WHERE token_hash = @h", ("h", _tokenB.TokenHash)))
            .Should().Be("0", "another tenant's token is invisible");

        await using (var update = new NpgsqlCommand("UPDATE identity.api_tokens SET last_used_at = now()", c, tx))
            (await update.ExecuteNonQueryAsync()).Should().Be(1, "only the token's own row can be updated");

        // Nothing else of the token's own tenant, and nothing of the other tenant, opens up.
        foreach (var table in new[] { "workspace.workspaces", "workspace.workspace_members", "work.boards", "account.accounts", "authz.access_grants", "identity.users", "workspace.workspace_invitations" })
            (await ScalarAsync(c, tx, $"SELECT count(*) FROM {table}")).Should().Be("0", $"{table} is closed to an api-token scope");

        await using (var delete = new NpgsqlCommand("DELETE FROM identity.api_tokens", c, tx))
            (await delete.ExecuteNonQueryAsync()).Should().Be(0, "no policy lets the scope delete a token");
    }

    [Fact]
    public async Task APurposeIsNotInterchangeable_AnApiTokenScopeIsNotAnInvitationScope_AndViceVersa()
    {
        var (c, tx) = await BeginAppAsync();
        await using (c)
        await using (tx)
        {
            (await ScalarAsync(c, tx, "SELECT count(*) FROM ops.enter_token_scope('workspace-invitation', @h, 0)", ("h", _tokenA.TokenHash)))
                .Should().Be("0", "an API token digest is not an invitation digest");
            (await ScalarAsync(c, tx, "SELECT count(*) FROM ops.enter_token_scope('api-token', @h, @v)",
                ("h", _a.InvitationTokenHash), ("v", _a.InvitationHashVersion)))
                .Should().Be("0", "an invitation digest is not an API token digest");
        }

        (c, tx) = await BeginAppAsync();
        await using (c)
        await using (tx)
        {
            await ScalarAsync(c, tx, "SELECT count(*) FROM ops.enter_token_scope('api-token', @h, 0)", ("h", _tokenA.TokenHash));
            (await ScalarAsync(c, tx, "SELECT ops.has_invitation_binding(@a, @w)", ("a", _a.AccountId), ("w", _a.WorkspaceId)))
                .Should().Be("False", "the invitation policies never admit an API token scope");
            (await ScalarAsync(c, tx, "SELECT ops.has_api_token_binding(@a, @w)", ("a", _a.AccountId), ("w", _a.WorkspaceId)))
                .Should().Be("True");
            (await ScalarAsync(c, tx, "SELECT ops.has_api_token_binding(@a, @w)", ("a", _b.AccountId), ("w", _b.WorkspaceId)))
                .Should().Be("False", "the scope is bound to the token's own tenant");
        }

        (c, tx) = await BeginAppAsync();
        await using (c)
        await using (tx)
        {
            await ScalarAsync(c, tx, "SELECT count(*) FROM ops.enter_token_scope('workspace-invitation', @h, @v)",
                ("h", _a.InvitationTokenHash), ("v", _a.InvitationHashVersion));
            (await ScalarAsync(c, tx, "SELECT ops.has_api_token_binding(@a, @w)", ("a", _a.AccountId), ("w", _a.WorkspaceId)))
                .Should().Be("False", "an invitation scope never admits the API token policies");
            (await ScalarAsync(c, tx, "SELECT count(*) FROM identity.api_tokens")).Should().Be("0");
        }
    }

    [Fact]
    public async Task TheScopeEndsWithItsTransaction()
    {
        await using var c = new NpgsqlConnection(_fixture.AppConnectionString);
        await c.OpenAsync();
        await using (var tx = await c.BeginTransactionAsync())
        {
            await ScalarAsync(c, tx, "SELECT count(*) FROM ops.enter_token_scope('api-token', @h, 0)", ("h", _tokenA.TokenHash));
            await tx.CommitAsync();
        }

        (await ScalarAsync(c, null!, "SELECT count(*) FROM identity.api_tokens")).Should().Be("0", "the next statement on the same connection has no scope");
        (await ScalarAsync(c, null!, "SELECT ops.current_request_scope()")).Should().Be("app");
    }
}
