using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Database-level tightenings of the runtime roles, proven against the production-shaped database. The
/// shared setup and the policy-DDL helper test live here; the other topics are partial files of this class.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed partial class DatabaseAccessTighteningTests : IAsyncLifetime
{
    private const string Denied = "42501";

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;

    public DatabaseAccessTighteningTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "tighten-a@example.com", "TightenA", "hash");

        // The tenant's owner also administers the account (an account-level grant), which the account's own
        // update policy requires.
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

    private async Task<string> OwnerScalarAsync(string sql, params (string, object)[] p)
    {
        await using var c = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await c.OpenAsync();
        await using var command = new NpgsqlCommand(sql, c);
        foreach (var (name, value) in p) command.Parameters.AddWithValue(name, value);
        return Convert.ToString(await command.ExecuteScalarAsync()) ?? "";
    }

    /// <summary>Runs <paramref name="work"/> as the application login inside the tenant scope of <paramref name="user"/>.</summary>
    private async Task AsAppAsync(Guid user, Func<NpgsqlConnection, NpgsqlTransaction, Task> work)
    {
        await using var c = new NpgsqlConnection(_fixture.AppConnectionString);
        await c.OpenAsync();
        await using var tx = await c.BeginTransactionAsync();
        await using (var scope = new NpgsqlCommand(
            "SELECT set_config('app.current_user_id', @u, true), set_config('app.current_account_id', @a, true), " +
            "set_config('app.current_workspace_id', @w, true), set_config('app.request_scope', 'app', true)", c, tx))
        {
            scope.Parameters.AddWithValue("u", user.ToString());
            scope.Parameters.AddWithValue("a", _a.AccountId.ToString());
            scope.Parameters.AddWithValue("w", _a.WorkspaceId.ToString());
            await scope.ExecuteNonQueryAsync();
        }

        await work(c, tx);
    }

    private static async Task<int> ExecAsync(NpgsqlConnection c, NpgsqlTransaction? tx, string sql)
    {
        await using var command = new NpgsqlCommand(sql, c, tx);
        return await command.ExecuteNonQueryAsync();
    }

    /// <summary>True when the statement is refused with a privilege or row-security violation.</summary>
    private static async Task<bool> IsDeniedAsync(NpgsqlConnection c, NpgsqlTransaction tx, string sql)
    {
        await ExecAsync(c, tx, "SAVEPOINT attempt");
        try
        {
            await ExecAsync(c, tx, sql);
            await ExecAsync(c, tx, "RELEASE SAVEPOINT attempt");
            return false;
        }
        catch (PostgresException e) when (e.SqlState == Denied)
        {
            await ExecAsync(c, tx, "ROLLBACK TO SAVEPOINT attempt");
            return true;
        }
    }

    [Fact]
    public async Task PolicyDdlHelpers_AreNotExecutableByAnyRuntimeLogin()
    {
        var executable = await OwnerScalarAsync("""
            SELECT coalesce(string_agg(p.proname || ' -> ' || r, ',' ORDER BY p.proname), '')
              FROM pg_proc p
              JOIN pg_namespace n ON n.oid = p.pronamespace AND n.nspname = 'ops'
              CROSS JOIN unnest(ARRAY['public', 'notrelix_app', 'notrelix_auth', 'notrelix_worker', 'notrelix_support_readonly']) AS r
             WHERE (p.proname IN ('create_policy', 'drop_all_policies_for_table', 'enable_rls_for_table') OR p.proname LIKE 'apply\_%')
               AND (r <> 'public' AND has_function_privilege(r, p.oid, 'EXECUTE')
                    OR r = 'public' AND EXISTS (SELECT 1 FROM aclexplode(coalesce(p.proacl, acldefault('f', p.proowner))) a
                                                 WHERE a.grantee = 0 AND a.privilege_type = 'EXECUTE'))
            """);
        executable.Should().BeEmpty("a SECURITY DEFINER policy writer must not be callable by a runtime login");

        // Each runtime role, actually calling the writer: the roles that are logins connect as themselves, the
        // others are assumed from the owner connection.
        var attempts = new (string Role, string Connection, bool SetRole)[]
        {
            ("notrelix_app", _fixture.AppConnectionString, false),
            ("notrelix_worker", _fixture.WorkerConnectionString, false),
            ("notrelix_auth", _fixture.OwnerConnectionString, true),
            ("notrelix_support_readonly", _fixture.OwnerConnectionString, true),
        };
        foreach (var (role, connection, setRole) in attempts)
        {
            await using var c = new NpgsqlConnection(connection);
            await c.OpenAsync();
            if (setRole) await ExecAsync(c, null, $"SET ROLE {role}");
            await using (var who = new NpgsqlCommand("SELECT current_user::text", c))
                (await who.ExecuteScalarAsync()).Should().Be(role);

            foreach (var call in new[]
                     {
                         "SELECT ops.create_policy('identity', 'users', 'p_pwn', 'SELECT', 'notrelix_app', 'true', NULL)",
                         "SELECT ops.drop_all_policies_for_table('work', 'boards')",
                         "SELECT ops.enable_rls_for_table('work', 'boards')",
                         "SELECT ops.apply_catalog_policies('work', 'boards')",
                     })
            {
                var act = async () => await ExecAsync(c, null, call);
                (await act.Should().ThrowAsync<PostgresException>($"{role}: {call}")).Which.SqlState.Should().Be(Denied, $"{role}: {call}");
            }
        }

        (await OwnerScalarAsync("SELECT count(*) FROM pg_policies WHERE policyname = 'p_pwn'")).Should().Be("0");
        (await OwnerScalarAsync("SELECT count(*) FROM pg_policies WHERE schemaname = 'work' AND tablename = 'boards'"))
            .Should().NotBe("0", "no call dropped the policies of work.boards");
    }
}
