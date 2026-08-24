using Npgsql;
using Notrelix.Integration.Tests.Containers;

namespace Notrelix.Integration.Tests.Performance;

/// <summary>
/// IA-TST-PERF-EVIDENCE — AccessFacts plan evidence over a representative large
/// tenant (freeze file 04 §8). Gated behind RUN_FREEZE_EVIDENCE=1 so normal CI
/// stays fast; the final freeze acceptance runs it explicitly from the final HEAD.
///
///   RUN_FREEZE_EVIDENCE=1 dotnet test --filter FullyQualifiedName~PipelineFreezeEvidenceTests
///
/// Writes the captured plan to /tmp/access-facts-large-explain.txt.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
[Trait("Category", "FreezeEvidence")]
public sealed class PipelineFreezeEvidenceTests : IAsyncLifetime
{
    private const int SeededMembers = 10_000;
    private const int SeededRules = 10_000;

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;
    private Guid _accountId;
    private Guid _workspaceId;

    public PipelineFreezeEvidenceTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        if (!IsEnabled)
        {
            return;
        }

        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
        await SeedLargeTenantAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static bool IsEnabled =>
        Environment.GetEnvironmentVariable("RUN_FREEZE_EVIDENCE") == "1";

    [Fact]
    public async Task AccessFacts_LargeTenant_PlanUsesIndexes_NoSequentialScan()
    {
        if (!IsEnabled)
        {
            return; // Evidence run is gated; see class doc.
        }

        // Canonical facts subselects over the seeded large tenant.
        var sql = """
            EXPLAIN (ANALYZE, BUFFERS)
            SELECT
              EXISTS (SELECT 1 FROM identity.users u WHERE u.id = @user_id AND u.deleted_at IS NULL),
              (SELECT am.role FROM account.account_members am
                 WHERE am.account_id = @account_id AND am.user_id = @user_id
                   AND am.status = 'Active' AND am.deleted_at IS NULL LIMIT 1),
              (SELECT wm.role FROM workspace.workspace_members wm
                 WHERE wm.account_id = @account_id AND wm.workspace_id = @workspace_id
                   AND wm.user_id = @user_id AND wm.status = 'Active' LIMIT 1),
              EXISTS (
                SELECT 1 FROM governance.resource_permissions rp
                 WHERE rp.account_id = @account_id AND rp.workspace_id = @workspace_id
                   AND rp.resource_type = @resource_type AND rp.resource_id = @resource_id
                   AND rp.subject_type = 'User' AND rp.subject_id = @user_id
                   AND rp.deleted_at IS NULL),
              COALESCE((
                SELECT jsonb_agg(jsonb_build_object('priority', pr.priority, 'effect', pr.effect) ORDER BY pr.priority)
                  FROM governance.permission_rules pr
                 WHERE pr.account_id = @account_id AND pr.workspace_id = @workspace_id
                   AND pr.status = 'Active'
                   AND pr.action = @action LIMIT 1000
              ), '[]'::jsonb)::text AS rules_text
            """;

        await using var conn = new NpgsqlConnection(_db.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("user_id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("account_id", _accountId);
        cmd.Parameters.AddWithValue("workspace_id", _workspaceId);
        cmd.Parameters.AddWithValue("resource_type", "work-management.board-item");
        cmd.Parameters.AddWithValue("resource_id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("action", "UpdateBoardItem");

        var plan = (await cmd.ExecuteScalarAsync())?.ToString() ?? string.Empty;
        var reportPath = Path.Combine(AppContext.BaseDirectory, "access-facts-large-explain.txt");
        await File.WriteAllTextAsync(reportPath,
            $"seeded workspace_members={SeededMembers} permission_rules={SeededRules}\n{plan}");

        plan.Should().NotBeNullOrWhiteSpace();
        File.Exists(reportPath).Should().BeTrue($"evidence file written to {{reportPath}}; enabled={{IsEnabled}}");
        plan.Should().NotContain("Seq Scan on workspace.workspace_members",
            "member lookup must stay index-backed at 10k cardinality");
        plan.Should().NotContain("Seq Scan on governance.permission_rules",
            "rule lookup must stay index-backed at 10k cardinality");
    }

    private async Task SeedLargeTenantAsync()
    {
        _accountId = Guid.NewGuid();
        _workspaceId = Guid.NewGuid();

        await using var conn = new NpgsqlConnection(_db.ConnectionString);
        await conn.OpenAsync();

        // One account + one workspace skeleton.
        var userId = Guid.NewGuid();
        await Exec(conn, """
            INSERT INTO identity.users (id, email, normalized_email, name, password_hash, has_password_credential, status, email_confirmed, created_at)
            VALUES (@u, 'perf@example.com', 'PERF@EXAMPLE.COM', 'Perf User', 'x', true, 'Active', true, now())
            """,
            ("u", userId));

        await Exec(conn, """
            INSERT INTO account.accounts (id, name, slug, type, status, created_at)
            VALUES (@a, 'Perf Account', 'perf-account', 'Team', 'Active', now())
            """,
            ("a", _accountId));

        await Exec(conn, """
            INSERT INTO workspace.workspaces (id, account_id, name, slug, is_personal, status, created_at)
            VALUES (@w, @a, 'Perf Workspace', 'perf-workspace', false, 'Active', now())
            """,
            ("w", _workspaceId),
            ("a", _accountId));

        await Exec(conn, """
            INSERT INTO account.account_members (id, account_id, user_id, role, status, created_at)
            VALUES (@id, @a, @u, 'Owner', 'Active', now())
            """,
            ("id", Guid.NewGuid()), ("a", _accountId), ("u", userId));

        await Exec(conn, """
            INSERT INTO workspace.workspace_members (id, account_id, workspace_id, user_id, role, status, created_at)
            VALUES (@id, @a, @w, @u, 'Owner', 'Active', now())
            """,
            ("id", Guid.NewGuid()), ("a", _accountId), ("w", _workspaceId), ("u", userId));

        // Bulk users fan-out: one identity per member slot.
        foreach (var chunk in Chunk(Enumerable.Range(0, SeededMembers), 500))
        {
            var values = string.Join(",",
                chunk.Select(i => $"""
                    ('{Guid.NewGuid()}'::uuid, 'perf-{i}@example.com', 'PERF-{i}@EXAMPLE.COM', 'Perf {i}', 'x', true, 'Active', true, now())
                    """));
            await Exec(conn, $"""
                INSERT INTO identity.users (id, email, normalized_email, name, password_hash, has_password_credential, status, email_confirmed, created_at)
                SELECT id, email, normalized_email, name, password_hash, has_password_credential, status, email_confirmed, created_at
                FROM (VALUES {values}) t(id, email, normalized_email, name, password_hash, has_password_credential, status, email_confirmed, created_at);
                """);
        }

        // Bulk member fan-out: 9,999 additional members on the same workspace.
        foreach (var chunk in Chunk(Enumerable.Range(0, SeededMembers - 1), 500))
        {
            var values = string.Join(",",
                chunk.Select(i => $"""
                    ('{Guid.NewGuid()}'::uuid, '{_accountId}'::uuid, '{_workspaceId}'::uuid, (SELECT id FROM identity.users OFFSET {i + 1} LIMIT 1), 'Member', 'Active', now())
                    """));
            await Exec(conn, $"""
                INSERT INTO workspace.workspace_members (id, account_id, workspace_id, user_id, role, status, created_at)
                SELECT id, account_id, workspace_id, user_id, role, status, created_at
                FROM (VALUES {values}) t(id, account_id, workspace_id, user_id, role, status, created_at);
                """);
        }

        // Permission rules fan-out on the same workspace.
        foreach (var chunk in Chunk(Enumerable.Range(0, SeededRules), 500))
        {
            var values = string.Join(",",
                chunk.Select(i => $"""
                    ('{Guid.NewGuid()}'::uuid, '{_accountId}'::uuid, '{_workspaceId}'::uuid, 'Active', {i % 5}, 'Allow', 'UpdateBoardItem', 'Workspace', 'User', '[]'::jsonb, now())
                    """));
            await Exec(conn, $"""
                INSERT INTO governance.permission_rules (id, account_id, workspace_id, status, priority, effect, action, scope_type, subject_type, condition_json, created_at)
                SELECT id, account_id, workspace_id, status, priority, effect, action, scope_type, subject_type, condition_json, created_at
                FROM (VALUES {values}) t(id, account_id, workspace_id, status, priority, effect, action, scope_type, subject_type, condition_json, created_at);
                """);
        }

        static IEnumerable<List<int>> Chunk(IEnumerable<int> source, int size)
        {
            var batch = new List<int>(size);
            foreach (var item in source)
            {
                batch.Add(item);
                if (batch.Count == size)
                {
                    yield return batch;
                    batch = new List<int>(size);
                }
            }

            if (batch.Count > 0)
            {
                yield return batch;
            }
        }

        static async Task Exec(NpgsqlConnection conn, string sql, params (string, object?)[] parameters)
        {
            await using var cmd = new NpgsqlCommand(sql, conn);
            foreach (var (name, value) in parameters)
            {
                cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
            }

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
