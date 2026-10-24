using Notrelix.Integration.Tests.Containers;
using Npgsql;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// Tables that carry no account or workspace column of their own inherit the scope of the row they belong
/// to (or of their owner). Rows are seeded as the owner; the application login then proves what it can read
/// and write inside a scope it holds and nothing outside it.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class DerivedScopePolicyTests : IAsyncLifetime
{
    private const string PostgresRlsViolation = "42501";

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;

    public DerivedScopePolicyTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "derived-a@example.com", "DerivedA", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "derived-b@example.com", "DerivedB", "hash");
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static string WorkspaceScope(AppRoleTenant t) => $"account:{t.AccountId:N}:workspace:{t.WorkspaceId:N}";
    private static string UserScope(AppRoleTenant t, Guid userId) => $"account:{t.AccountId:N}:user:{userId:N}";

    private async Task OwnerExecAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>Runs <paramref name="work"/> on the application login inside the tenant scope of <paramref name="tenant"/>.</summary>
    private async Task<T> AsAppAsync<T>(AppRoleTenant tenant, Func<NpgsqlConnection, NpgsqlTransaction, Task<T>> work)
    {
        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var scope = new NpgsqlCommand(
            "SELECT set_config('app.current_user_id', @u, true), set_config('app.current_account_id', @a, true), " +
            "set_config('app.current_workspace_id', @w, true), set_config('app.request_scope', 'app', true)",
            connection, transaction))
        {
            scope.Parameters.AddWithValue("u", tenant.User.Id.ToString());
            scope.Parameters.AddWithValue("a", tenant.AccountId.ToString());
            scope.Parameters.AddWithValue("w", tenant.WorkspaceId.ToString());
            await scope.ExecuteNonQueryAsync();
        }

        return await work(connection, transaction);
    }

    private static async Task<long> CountAsync(NpgsqlConnection c, NpgsqlTransaction t, string sql)
    {
        await using var command = new NpgsqlCommand(sql, c, t);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<bool> InsertIsDeniedAsync(NpgsqlConnection c, NpgsqlTransaction t, string sql)
    {
        await using var savepoint = new NpgsqlCommand("SAVEPOINT attempt", c, t);
        await savepoint.ExecuteNonQueryAsync();
        try
        {
            await using var command = new NpgsqlCommand(sql, c, t);
            await command.ExecuteNonQueryAsync();
            return false;
        }
        catch (PostgresException e) when (e.SqlState == PostgresRlsViolation)
        {
            await using var rollback = new NpgsqlCommand("ROLLBACK TO SAVEPOINT attempt", c, t);
            await rollback.ExecuteNonQueryAsync();
            return true;
        }
    }

    private static string IdempotencyInsert(string scope, string key) =>
        "INSERT INTO ops.idempotency_records (id, scope, operation, key_hash, request_hash, state, created_at, expires_at) " +
        $"VALUES (gen_random_uuid(), '{scope}', 'op', '{key}', 'r', 'Started', now(), now() + interval '1 hour')";

    [Fact]
    public async Task IdempotencyRecords_AreVisibleAndWritableOnlyInsideAScopeTheCallerHolds()
    {
        await OwnerExecAsync(IdempotencyInsert(WorkspaceScope(_b), "owner-b"));
        await OwnerExecAsync(IdempotencyInsert("system", "owner-system"));

        await AsAppAsync(_a, async (c, t) =>
        {
            (await InsertIsDeniedAsync(c, t, IdempotencyInsert(WorkspaceScope(_a), "a-workspace"))).Should().BeFalse("the caller's own workspace partition");
            (await InsertIsDeniedAsync(c, t, IdempotencyInsert($"account:{_a.AccountId:N}", "a-account"))).Should().BeFalse("the caller's own account partition");
            (await InsertIsDeniedAsync(c, t, IdempotencyInsert(UserScope(_a, _a.User.Id), "a-user"))).Should().BeFalse("the caller's own user partition");

            (await InsertIsDeniedAsync(c, t, IdempotencyInsert(WorkspaceScope(_b), "a-into-b"))).Should().BeTrue("another tenant's partition");
            (await InsertIsDeniedAsync(c, t, IdempotencyInsert(UserScope(_a, _b.User.Id), "a-as-b"))).Should().BeTrue("another person's user partition");
            (await InsertIsDeniedAsync(c, t, IdempotencyInsert("system", "a-system"))).Should().BeTrue("the system partition is the worker's");
            (await InsertIsDeniedAsync(c, t, IdempotencyInsert("whatever", "a-free-text"))).Should().BeTrue("a partition that is not tenant-qualified");

            (await CountAsync(c, t, "SELECT count(*) FROM ops.idempotency_records WHERE key_hash IN ('owner-b', 'owner-system')"))
                .Should().Be(0, "neither another tenant's record nor the system record is readable");
            (await CountAsync(c, t, "SELECT count(*) FROM ops.idempotency_records WHERE key_hash LIKE 'a-%'"))
                .Should().Be(3);
            return 0;
        });
    }

    [Fact]
    public async Task MfaRecoveryCodes_AreTheOwnersOwn_ThroughTheirBatch()
    {
        var batchA = Guid.NewGuid();
        var batchB = Guid.NewGuid();
        await OwnerExecAsync(
            "INSERT INTO identity.mfa_recovery_batches (id, created_at, user_id, version) VALUES (@a, now(), @ua, 1), (@b, now(), @ub, 1);" +
            "INSERT INTO identity.mfa_recovery_codes (id, batch_id, code_hash) VALUES (gen_random_uuid(), @a, 'ha'), (gen_random_uuid(), @b, 'hb')",
            ("a", batchA), ("b", batchB), ("ua", _a.User.Id), ("ub", _b.User.Id));

        await AsAppAsync(_a, async (c, t) =>
        {
            (await CountAsync(c, t, "SELECT count(*) FROM identity.mfa_recovery_batches")).Should().Be(1);
            (await CountAsync(c, t, "SELECT count(*) FROM identity.mfa_recovery_codes WHERE code_hash = 'ha'")).Should().Be(1);
            (await CountAsync(c, t, "SELECT count(*) FROM identity.mfa_recovery_codes WHERE code_hash = 'hb'"))
                .Should().Be(0, "another person's recovery code is invisible");
            (await InsertIsDeniedAsync(c, t,
                $"INSERT INTO identity.mfa_recovery_codes (id, batch_id, code_hash) VALUES (gen_random_uuid(), '{batchB}', 'x')"))
                .Should().BeTrue("a code cannot be added to another person's batch");
            return 0;
        });
    }

    [Fact]
    public async Task NotificationRecipients_AreTheRecipientsOwn()
    {
        await OwnerExecAsync(
            "SET session_replication_role = replica;" +
            "INSERT INTO notifications.notification_recipients (id, account_id, workspace_id, notification_id, recipient_user_id, status, delivery_policy_json, created_at) VALUES " +
            "(gen_random_uuid(), @aa, @aw, gen_random_uuid(), @ua, 'Unread', '{}', now()), " +
            "(gen_random_uuid(), @ba, @bw, gen_random_uuid(), @ub, 'Unread', '{}', now())",
            ("aa", _a.AccountId), ("aw", _a.WorkspaceId), ("ua", _a.User.Id),
            ("ba", _b.AccountId), ("bw", _b.WorkspaceId), ("ub", _b.User.Id));

        await AsAppAsync(_a, async (c, t) =>
        {
            (await CountAsync(c, t, "SELECT count(*) FROM notifications.notification_recipients"))
                .Should().Be(1, "a person reads only their own recipient rows");
            return 0;
        });
    }

    [Fact]
    public async Task IntegrationSecretBlobs_CanBeStagedInsideATenantScope_AndAreUnreadableUnlessAConnectionOfTheScopeReferencesThem()
    {
        var blob = Guid.NewGuid();
        await AsAppAsync(_a, async (c, t) =>
        {
            (await InsertIsDeniedAsync(c, t,
                $"INSERT INTO integration.integration_secret_blobs (id, created_at, encrypted_payload, revoked) VALUES ('{blob}', now(), 'x', false)"))
                .Should().BeFalse("a blob is staged inside the caller's own tenant scope");
            (await CountAsync(c, t, "SELECT count(*) FROM integration.integration_secret_blobs"))
                .Should().Be(0, "an unreferenced blob is not readable, even by the scope that staged it");
            await t.CommitAsync();
            return 0;
        });

        await using var connection = new NpgsqlConnection(_fixture.AppConnectionString);
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        (await InsertIsDeniedAsync(connection, transaction,
            $"INSERT INTO integration.integration_secret_blobs (id, created_at, encrypted_payload, revoked) VALUES ('{Guid.NewGuid()}', now(), 'x', false)"))
            .Should().BeTrue("without a scope the application login cannot stage a blob");
    }
}
