using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.API;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Admin-driven grant writes under the application role. The application role has no write policy on
/// <c>authz.access_grants</c>; adding, removing, suspending, re-activating and re-roling a member, creating a
/// workspace and transferring ownership write the grant through the owner-defined bounded functions, which
/// land only inside the account and workspace the request is scoped to. Application authorization stays the
/// authority; the function is the bound underneath it.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class AdminGrantWriteTests : IAsyncLifetime
{
    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private User _bob = null!;

    public AdminGrantWriteTests(AppRoleTestContainer fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "a-grant@example.com", "Aaa", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "b-grant@example.com", "Bbb", "hash");
        _bob = await AppRoleUsers.AddAsync(_fixture, "bob-grant@example.com", "Bob", "hash");

        // Tenant A's owner is also the account's owner (an account-level grant and member row), which the
        // workspace creation authorizes against.
        var now = DateTimeOffset.UtcNow;
        var system = new FakeCurrentTenantContext();
        system.SetSystem();
        await using var ctx = _fixture.CreateOwnerContext(system);
        ctx.AccountMembers.Add(AccountMember.Create(_a.AccountId, _a.User.Id, AccountRole.Owner, _a.User.Id, now));
        ctx.AccessGrants.Add(new AccessGrant(
            accountId: _a.AccountId, workspaceId: null, userId: _a.User.Id, sourceContext: "Account",
            membershipStatus: "Active", roleCodes: ["Owner"], permissionCodes: [], isAccountAdmin: true,
            isWorkspaceAdmin: false, grantedAt: now));
        await ctx.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static string Jwt(WebApplicationFactory<Program> factory, User user)
    {
        using var scope = factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(user);
    }

    private HttpClient ClientFor(WebApplicationFactory<Program> factory, User user)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", Jwt(factory, user));
        return client;
    }

    private async Task<string> OwnerScalarAsync(string sql, params (string, object)[] p)
    {
        await using var c = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await c.OpenAsync();
        return await ScalarAsync(c, null, sql, p);
    }

    private static async Task<string> ScalarAsync(NpgsqlConnection c, NpgsqlTransaction? tx, string sql, params (string, object)[] p)
    {
        await using var command = new NpgsqlCommand(sql, c, tx);
        foreach (var (name, value) in p)
            command.Parameters.AddWithValue(name, value);
        return Convert.ToString(await command.ExecuteScalarAsync()) ?? "";
    }

    private Task<string> GrantOfAsync(Guid workspaceId, Guid userId, string column) =>
        OwnerScalarAsync(
            $"SELECT {column} FROM authz.access_grants WHERE workspace_id = @w AND user_id = @u",
            ("w", workspaceId), ("u", userId));

    private async Task<HttpStatusCode> WorkspaceReadAsync(WebApplicationFactory<Program> factory, User user, Guid workspaceId)
    {
        using var client = ClientFor(factory, user);
        return (await client.GetAsync($"/api/v1/workspaces/{workspaceId}")).StatusCode;
    }

    // ------------------------------------------------------------------------------------------------
    // The use cases, over the real host as the application role
    // ------------------------------------------------------------------------------------------------

    [Fact]
    public async Task TheMemberLifecycle_UnderTheAppRole_WritesAndEndsTheGrantsThroughTheBoundedFunctions()
    {
        using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);
        using var owner = ClientFor(factory, _a.User);

        (await WorkspaceReadAsync(factory, _bob, _a.WorkspaceId)).Should().NotBe(HttpStatusCode.OK, "a stranger has no access yet");

        // Add
        var add = await owner.PostAsJsonAsync($"/api/v1/workspaces/{_a.WorkspaceId}/members/add",
            new { userId = _bob.Id, role = "Member" });
        add.IsSuccessStatusCode.Should().BeTrue(await add.Content.ReadAsStringAsync());
        (await GrantOfAsync(_a.WorkspaceId, _bob.Id, "role_codes[1] || ':' || is_workspace_admin || ':' || (revoked_at IS NULL)"))
            .Should().Be("Member:false:true");
        (await WorkspaceReadAsync(factory, _bob, _a.WorkspaceId)).Should().Be(HttpStatusCode.OK, "the new grant admits the member");

        // Change role
        var role = await owner.PatchAsJsonAsync($"/api/v1/workspaces/{_a.WorkspaceId}/members/{_bob.Id}", new { role = "Admin" });
        role.IsSuccessStatusCode.Should().BeTrue(await role.Content.ReadAsStringAsync());
        (await GrantOfAsync(_a.WorkspaceId, _bob.Id, "role_codes[1] || ':' || is_workspace_admin")).Should().Be("Admin:true");

        // Suspend
        var suspend = await owner.PostAsync($"/api/v1/workspaces/{_a.WorkspaceId}/members/{_bob.Id}/suspend", null);
        suspend.IsSuccessStatusCode.Should().BeTrue(await suspend.Content.ReadAsStringAsync());
        (await GrantOfAsync(_a.WorkspaceId, _bob.Id, "revoked_at IS NOT NULL")).Should().Be("True");
        (await WorkspaceReadAsync(factory, _bob, _a.WorkspaceId)).Should().NotBe(HttpStatusCode.OK, "a suspended member is out");

        // Activate
        var activate = await owner.PostAsync($"/api/v1/workspaces/{_a.WorkspaceId}/members/{_bob.Id}/activate", null);
        activate.IsSuccessStatusCode.Should().BeTrue(await activate.Content.ReadAsStringAsync());
        (await GrantOfAsync(_a.WorkspaceId, _bob.Id, "revoked_at IS NULL")).Should().Be("True");

        // Remove
        var remove = await owner.DeleteAsync($"/api/v1/workspaces/{_a.WorkspaceId}/members/{_bob.Id}");
        remove.IsSuccessStatusCode.Should().BeTrue(await remove.Content.ReadAsStringAsync());
        (await GrantOfAsync(_a.WorkspaceId, _bob.Id, "revoked_at IS NOT NULL")).Should().Be("True");
        (await WorkspaceReadAsync(factory, _bob, _a.WorkspaceId)).Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ALeavingMember_RevokesTheirOwnGrant()
    {
        var bob = await AppRoleMembers.AddAsync(_fixture, _a, "Leaver", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash");
        using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);
        (await WorkspaceReadAsync(factory, bob, _a.WorkspaceId)).Should().Be(HttpStatusCode.OK);

        using var client = ClientFor(factory, bob);
        var leave = await client.DeleteAsync($"/api/v1/account/workspaces/{_a.WorkspaceId}");
        leave.IsSuccessStatusCode.Should().BeTrue(await leave.Content.ReadAsStringAsync());
        (await GrantOfAsync(_a.WorkspaceId, bob.Id, "revoked_at IS NOT NULL")).Should().Be("True");
        (await WorkspaceReadAsync(factory, bob, _a.WorkspaceId)).Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreatingAWorkspace_UnderTheAppRole_WritesTheCreatorsOwnerGrant()
    {
        using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);
        using var client = ClientFor(factory, _a.User);

        var response = await client.PostAsJsonAsync($"/api/v1/accounts/{_a.AccountId}/workspaces",
            new { name = "Second Space", description = "d", isPersonal = false });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var created = await response.Content.ReadFromJsonAsync<Guid>();

        (await GrantOfAsync(created, _a.User.Id, "role_codes[1] || ':' || is_workspace_admin || ':' || (revoked_at IS NULL)"))
            .Should().Be("Owner:true:true");
        (await WorkspaceReadAsync(factory, _a.User, created)).Should().Be(HttpStatusCode.OK, "the creator can use the workspace right away");
    }

    [Fact]
    public async Task TransferringOwnership_UnderTheAppRole_MovesTheOwnerGrant()
    {
        var bob = await AppRoleMembers.AddAsync(_fixture, _a, "Heir", WorkspaceRole.Admin, WorkspaceMemberStatus.Active, "hash");
        using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);
        using var client = ClientFor(factory, _a.User);
        var version = await OwnerScalarAsync("SELECT version FROM workspace.workspaces WHERE id = @w", ("w", _a.WorkspaceId));

        var response = await client.PostAsJsonAsync($"/api/v1/workspaces/{_a.WorkspaceId}/transfer-ownership",
            new { newOwnerId = bob.Id, expectedVersion = long.Parse(version) });
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());

        (await GrantOfAsync(_a.WorkspaceId, bob.Id, "role_codes[1]")).Should().Be("Owner");
        (await GrantOfAsync(_a.WorkspaceId, _a.User.Id, "role_codes[1]")).Should().Be("Admin");
    }

    [Fact]
    public async Task AnotherTenantsOwner_CannotChangeThisTenantsMembers()
    {
        using var factory = AppRoleHost.CreateFactory(_fixture, _fixture.AppConnectionString);
        using var client = ClientFor(factory, _b.User);

        var add = await client.PostAsJsonAsync($"/api/v1/workspaces/{_a.WorkspaceId}/members/add",
            new { userId = _bob.Id, role = "Member" });
        add.IsSuccessStatusCode.Should().BeFalse();
        (await OwnerScalarAsync("SELECT count(*) FROM authz.access_grants WHERE user_id = @u", ("u", _bob.Id))).Should().Be("0");
    }

    // ------------------------------------------------------------------------------------------------
    // The bound, proven directly on the function under the application role
    // ------------------------------------------------------------------------------------------------

    private const string Upsert = "SELECT ops.upsert_access_grant(@a, @w, @u, @r, now())";
    private const string Revoke = "SELECT ops.revoke_access_grant(@a, @w, @u, now())";

    private async Task<(NpgsqlConnection, NpgsqlTransaction)> BeginAsync(
        Guid? user, Guid? account, Guid? workspace, string scope = "app")
    {
        var c = new NpgsqlConnection(_fixture.AppConnectionString);
        await c.OpenAsync();
        var tx = await c.BeginTransactionAsync();
        await ScalarAsync(c, tx,
            "SELECT set_config('app.current_user_id', @u, true), set_config('app.current_account_id', @a, true), " +
            "set_config('app.current_workspace_id', @w, true), set_config('app.request_scope', @s, true)",
            ("u", user?.ToString() ?? ""), ("a", account?.ToString() ?? ""), ("w", workspace?.ToString() ?? ""), ("s", scope));
        return (c, tx);
    }

    private static async Task ExpectDeniedAsync(NpgsqlConnection c, NpgsqlTransaction tx, string sql, params (string, object)[] p)
    {
        await using var savepoint = new NpgsqlCommand("SAVEPOINT s", c, tx);
        await savepoint.ExecuteNonQueryAsync();
        var act = () => ScalarAsync(c, tx, sql, p);
        (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().BeOneOf("42501", "22023");
        await using var rollback = new NpgsqlCommand("ROLLBACK TO SAVEPOINT s", c, tx);
        await rollback.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task WithoutAScope_NoGrantCanBeWrittenOrRevoked()
    {
        var (c, tx) = await BeginAsync(null, null, null);
        await using (c)
        await using (tx)
        {
            await ExpectDeniedAsync(c, tx, Upsert, ("a", _a.AccountId), ("w", _a.WorkspaceId), ("u", _bob.Id), ("r", "Member"));
            await ExpectDeniedAsync(c, tx, Revoke, ("a", _a.AccountId), ("w", _a.WorkspaceId), ("u", _a.User.Id));
        }
    }

    [Fact]
    public async Task AGrantLandsOnlyInsideTheScopesOwnAccountAndWorkspace()
    {
        var (c, tx) = await BeginAsync(_a.User.Id, _a.AccountId, _a.WorkspaceId);
        await using (c)
        await using (tx)
        {
            await ScalarAsync(c, tx, Upsert, ("a", _a.AccountId), ("w", _a.WorkspaceId), ("u", _bob.Id), ("r", "Member"));
            await ExpectDeniedAsync(c, tx, Upsert, ("a", _b.AccountId), ("w", _b.WorkspaceId), ("u", _bob.Id), ("r", "Member"));
            await ExpectDeniedAsync(c, tx, Upsert, ("a", _a.AccountId), ("w", _b.WorkspaceId), ("u", _bob.Id), ("r", "Member"));
            await ExpectDeniedAsync(c, tx, Upsert, ("a", _b.AccountId), ("w", _a.WorkspaceId), ("u", _bob.Id), ("r", "Member"));
            await ExpectDeniedAsync(c, tx, Revoke, ("a", _b.AccountId), ("w", _b.WorkspaceId), ("u", _b.User.Id));
        }
    }

    [Fact]
    public async Task OwnerIsGrantedOnlyByTheCurrentOwner_AndAnAdminCannotSelfPromote()
    {
        var admin = await AppRoleMembers.AddAsync(_fixture, _a, "Adm", WorkspaceRole.Admin, WorkspaceMemberStatus.Active, "hash");

        var (c, tx) = await BeginAsync(admin.Id, _a.AccountId, _a.WorkspaceId);
        await using (c)
        await using (tx)
        {
            await ExpectDeniedAsync(c, tx, Upsert, ("a", _a.AccountId), ("w", _a.WorkspaceId), ("u", admin.Id), ("r", "Owner"));
            await ExpectDeniedAsync(c, tx, Upsert, ("a", _a.AccountId), ("w", _a.WorkspaceId), ("u", _bob.Id), ("r", "Owner"));
            await ScalarAsync(c, tx, Upsert, ("a", _a.AccountId), ("w", _a.WorkspaceId), ("u", _bob.Id), ("r", "Guest"));
        }

        (c, tx) = await BeginAsync(_a.User.Id, _a.AccountId, _a.WorkspaceId);
        await using (c)
        await using (tx)
        {
            await ScalarAsync(c, tx, Upsert, ("a", _a.AccountId), ("w", _a.WorkspaceId), ("u", admin.Id), ("r", "Owner"));
        }
    }

    [Fact]
    public async Task ANewWorkspaceOwnerGrant_IsForTheCallerOnly_OfANewWorkspace_InTheirOwnAccount()
    {
        var fresh = Guid.NewGuid();
        var (c, tx) = await BeginAsync(_a.User.Id, _a.AccountId, null);
        await using (c)
        await using (tx)
        {
            await ExpectDeniedAsync(c, tx, Upsert, ("a", _a.AccountId), ("w", fresh), ("u", _bob.Id), ("r", "Owner"));
            await ExpectDeniedAsync(c, tx, Upsert, ("a", _a.AccountId), ("w", _a.WorkspaceId), ("u", _a.User.Id), ("r", "Owner"));
            await ExpectDeniedAsync(c, tx, Upsert, ("a", _b.AccountId), ("w", fresh), ("u", _a.User.Id), ("r", "Owner"));
            await ScalarAsync(c, tx, Upsert, ("a", _a.AccountId), ("w", fresh), ("u", _a.User.Id), ("r", "Owner"));
        }

        // A caller with no access to the account cannot create into it.
        (c, tx) = await BeginAsync(_bob.Id, _a.AccountId, null);
        await using (c)
        await using (tx)
        {
            await ExpectDeniedAsync(c, tx, Upsert, ("a", _a.AccountId), ("w", Guid.NewGuid()), ("u", _bob.Id), ("r", "Owner"));
        }
    }

    [Fact]
    public async Task TheAccountLevelGrant_IsMemberOnly_AndNeverReplacesAnAdministratorsGrant()
    {
        var (c, tx) = await BeginAsync(_a.User.Id, _a.AccountId, _a.WorkspaceId);
        await using (c)
        await using (tx)
        {
            await ExpectDeniedAsync(c, tx, Upsert, ("a", _a.AccountId), ("w", DBNull.Value), ("u", _bob.Id), ("r", "Admin"));
            await ScalarAsync(c, tx, Upsert, ("a", _a.AccountId), ("w", DBNull.Value), ("u", _bob.Id), ("r", "Member"));
            await ScalarAsync(c, tx, Upsert, ("a", _a.AccountId), ("w", DBNull.Value), ("u", _a.User.Id), ("r", "Member"));
            await tx.CommitAsync();
        }

        (await OwnerScalarAsync(
            "SELECT is_account_admin::text || ':' || role_codes[1] FROM authz.access_grants WHERE workspace_id IS NULL AND user_id = @u",
            ("u", _a.User.Id))).Should().Be("true:Owner", "the Member write did not downgrade the account owner");
        (await OwnerScalarAsync(
            "SELECT role_codes[1] FROM authz.access_grants WHERE workspace_id IS NULL AND user_id = @u", ("u", _bob.Id)))
            .Should().Be("Member");
    }

    [Fact]
    public async Task OtherScopes_CannotUseTheTenantPaths()
    {
        foreach (var scope in new[] { "user", "token", "credential" })
        {
            var (c, tx) = await BeginAsync(_a.User.Id, _a.AccountId, _a.WorkspaceId, scope);
            await using (c)
            await using (tx)
            {
                await ExpectDeniedAsync(c, tx, Upsert, ("a", _a.AccountId), ("w", _a.WorkspaceId), ("u", _bob.Id), ("r", "Member"));
                await ExpectDeniedAsync(c, tx, Revoke, ("a", _a.AccountId), ("w", _a.WorkspaceId), ("u", _a.User.Id));
            }
        }
    }

    [Fact]
    public async Task TheApplicationRole_StillHasNoBroadWritePolicyOnGrants()
    {
        var (c, tx) = await BeginAsync(_a.User.Id, _a.AccountId, _a.WorkspaceId);
        await using (c)
        await using (tx)
        {
            await ExpectDeniedAsync(c, tx,
                "INSERT INTO authz.access_grants (id, account_id, workspace_id, user_id, source_context, membership_status, role_codes, permission_codes, is_account_admin, is_workspace_admin, granted_at, updated_at) " +
                "VALUES (gen_random_uuid(), @a, @w, @u, 'Workspace', 'Active', ARRAY['Owner'], ARRAY[]::text[], false, true, now(), now())",
                ("a", _a.AccountId), ("w", _a.WorkspaceId), ("u", _bob.Id));

            // The one update the role holds is ending its OWN active grant; rewriting it is refused.
            await ExpectDeniedAsync(c, tx,
                "UPDATE authz.access_grants SET role_codes = ARRAY['Owner'], is_workspace_admin = true WHERE workspace_id = @w",
                ("w", _a.WorkspaceId));

            await using (var delete = new NpgsqlCommand("DELETE FROM authz.access_grants WHERE workspace_id = @w", c, tx))
            {
                delete.Parameters.AddWithValue("w", _a.WorkspaceId);
                (await delete.ExecuteNonQueryAsync()).Should().Be(0);
            }
        }
    }
}
