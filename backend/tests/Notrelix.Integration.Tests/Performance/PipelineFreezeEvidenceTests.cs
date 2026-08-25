using Npgsql;
using System.Text.Json;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Infrastructure.Data.Authz;

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
    private Guid _userId;

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

        var actingUser = _userId;

        // Canonical production SQL verbatim (AccessFactsQuery.Sql) under FORMAT
        // JSON so the FULL node tree is captured in one row/column.
        var explainSql = "EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON) " + AccessFactsQuery.Sql;

        await using var conn = new NpgsqlConnection(_db.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(explainSql, conn);
        cmd.Parameters.AddWithValue("user_id", actingUser);
        cmd.Parameters.AddWithValue("account_id", _accountId);
        cmd.Parameters.AddWithValue("workspace_id", _workspaceId);
        cmd.Parameters.AddWithValue("resource_type", "work-management.board-item");
        cmd.Parameters.AddWithValue("resource_id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("resource_was_located", true);
        cmd.Parameters.AddWithValue("action", "UpdateBoardItem");
        cmd.Parameters.Add(new NpgsqlParameter("feature_code", NpgsqlTypes.NpgsqlDbType.Text) { Value = DBNull.Value });
        cmd.Parameters.AddWithValue("feature_amount", 0);
        cmd.Parameters.AddWithValue("now", DateTimeOffset.UtcNow);

        string jsonText;
        try
        {
            jsonText = (await cmd.ExecuteScalarAsync())?.ToString()
                ?? throw new InvalidOperationException("EXPLAIN returned no plan");
        }
        catch (Exception)
        {
            await File.WriteAllTextAsync(Path.Combine(AppContext.BaseDirectory, "access-facts-failing-sql.txt"), explainSql);
            throw;
        }

        using var planDoc = JsonDocument.Parse(jsonText);
        var rootPlan = planDoc.RootElement[0].GetProperty("Plan");
        var reportPath = Path.Combine(AppContext.BaseDirectory, "access-facts-large-explain.txt");

        var lines = new List<string>
        {
            $"seeded workspace_members={SeededMembers} permission_rules={SeededRules} sqlLen={explainSql.Length}",
            explainSql,
            JsonSerializer.Serialize(rootPlan, new JsonSerializerOptions { WriteIndented = true }),
        };
        await File.WriteAllLinesAsync(reportPath, lines);

        var violations = new List<string>();
        var totalActualTime = 0.0;
        Traverse(rootPlan, node =>
        {
            var nodeType = node.GetProperty("Node Type").GetString();
            totalActualTime += node.TryGetProperty("Actual Total Time", out var t) ? t.GetDouble() : 0;

            var relation = node.TryGetProperty("Relation Name", out var r) ? r.GetString() : null;
            if (nodeType == "Seq Scan" && relation is not null &&
                relation is "workspace_members" or "permission_rules" or "resource_permissions" or "users" or "accounts" or "workspaces")
            {
                violations.Add($"Seq Scan on {relation}");
            }
        });

        violations.Should().BeEmpty(
            "hot facts predicates must stay index-backed at 10k cardinality; violations: {0}",
            string.Join("; ", violations));

        File.Exists(reportPath).Should().BeTrue();
        totalActualTime.Should().BeGreaterThan(0);

        static void Traverse(JsonElement node, Action<JsonElement> visit)
        {
            visit(node);
            if (node.TryGetProperty("Plans", out var children))
            {
                foreach (var child in children.EnumerateArray())
                {
                    Traverse(child, visit);
                }
            }
        }
    }

    private async Task SeedLargeTenantAsync()
    {
        _accountId = Guid.NewGuid();
        _workspaceId = Guid.NewGuid();

        await using var conn = new NpgsqlConnection(_db.ConnectionString);
        await conn.OpenAsync();

        // One account + one workspace skeleton.
        var userId = Guid.NewGuid(); _userId = userId;
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
                    ('{Guid.NewGuid()}', '{_accountId}', '{_workspaceId}', 'Active', {i % 5}, 'Allow', 'UpdateBoardItem', 'Workspace', 'User', '{_userId}', '[]'::jsonb, now())
                    """));
            await Exec(conn, $"""
                INSERT INTO governance.permission_rules (id, account_id, workspace_id, status, priority, effect, action, scope_type, subject_type, subject_id, condition_json, created_at)
                VALUES {values};
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

            try
            {
                await cmd.ExecuteNonQueryAsync();
            }
            catch (PostgresException)
            {
                await File.WriteAllTextAsync(
                    Path.Combine(AppContext.BaseDirectory, "access-facts-failing-sql.txt"), sql);
                throw;
            }
        }
    }
}
