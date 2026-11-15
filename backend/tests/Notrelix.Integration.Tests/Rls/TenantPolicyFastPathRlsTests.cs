using System.Text.RegularExpressions;
using Npgsql;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;

namespace Notrelix.Integration.Tests.Rls;

/// <summary>
/// 031_tenant_policy_fast_path.sql gives every table whose application policy is exactly
/// <c>ops.has_workspace_access(account_id, workspace_id)</c> the same fast path the chat tables got in 030. For each
/// of those tables (the list below is checked against the catalog, so none drops out silently) one row is seeded in
/// each of two Workspaces, and for each caller the rows the policy lets the application login read must equal the rows
/// the original function accepts, asked as the same login in the same session. The suspended, revoked, removed and
/// no-user callers must read nothing.
/// </summary>
[Collection("DatabaseAppRole")]
[Trait("Category", "RlsAppRole")]
public sealed class TenantPolicyFastPathRlsTests : IAsyncLifetime
{
    /// <summary>Class A: the application policy is exactly the plain workspace rule (see 031).</summary>
    private static readonly string[] ClassA =
    [
        "account.workspace_routes",
        "automation.ai_agent_runs", "automation.ai_agents", "automation.automation_executions",
        "automation.automation_rules", "automation.scheduled_jobs",
        "billing.entitlements", "billing.feature_usage_ledger", "billing.invoices", "billing.payment_methods",
        "billing.subscriptions", "billing.usage_metrics", "billing.workspace_feature_usages",
        "collab.attachment_pending_uploads", "collab.attachments", "collab.comments", "collab.mentions",
        "collab.presence_sessions", "collab.reactions", "collab.resource_read_states", "collab.resource_watchers",
        "docs.blocks", "docs.document_versions", "docs.resource_links",
        "governance.custom_role_changes", "governance.field_permissions", "governance.permission_rules",
        "governance.permission_templates", "governance.resource_access_changes", "governance.resource_permissions",
        "governance.share_links", "governance.workspace_policies",
        "integration.outbound_delivery_log_entries", "integration.webhook_deliveries", "integration.webhook_subscriptions",
        "reporting.dashboard_sources", "reporting.dashboard_widgets", "reporting.dashboards",
        "reporting.reporting_snapshots", "reporting.workspace_work_item_placements",
        "search.search_documents",
        "work.approval_requests", "work.board_fields", "work.board_groups", "work.board_item_connections",
        "work.board_item_key_aliases", "work.board_item_labels", "work.board_item_links", "work.board_item_members",
        "work.board_item_reminders", "work.board_item_views", "work.board_item_votes", "work.board_items",
        "work.board_relations", "work.board_subscribers", "work.board_templates", "work.board_view_pins",
        "work.board_view_user_preferences", "work.board_views", "work.checklists", "work.form_questions",
        "work.form_submissions", "work.forms", "work.item_dependencies", "work.item_templates", "work.labels",
        "work.mirror_value_snapshots", "work.saved_filters", "work.time_tracking_entries", "work.workload_allocations",
        "workspace.space_members", "workspace.space_paths", "workspace.spaces", "workspace.team_members",
        "workspace.teams", "workspace.workspace_members",
    ];

    /// <summary>Class B tables that carry the workspace rule next to something else: they keep their old policy.</summary>
    private static readonly string[] ClassBWithWorkspaceRule =
    [
        "docs.pages", "identity.api_tokens", "integration.calendar_integrations", "integration.inbound_webhook_receipts",
        "integration.integration_connections", "messaging.outbox_messages", "work.boards", "workspace.workspace_invitations",
    ];

    /// <summary>Columns whose generic value breaks a check constraint or a unique index, per table.</summary>
    private static readonly Dictionary<string, Dictionary<string, string>> Overrides = new()
    {
    };

    private readonly AppRoleTestContainer _fixture;
    private AppRoleTenant _a = null!;
    private AppRoleTenant _b = null!;
    private Guid _activeMember;
    private Guid _suspended;
    private Guid _removed;
    private Guid _revoked;
    private Guid _invitee;

    public TenantPolicyFastPathRlsTests(AppRoleTestContainer fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _a = await AppRoleTenants.SeedAsync(_fixture, "tenant-fast-a@example.com", "TFastA", "hash");
        _b = await AppRoleTenants.SeedAsync(_fixture, "tenant-fast-b@example.com", "TFastB", "hash");
        _activeMember = (await AppRoleMembers.AddAsync(_fixture, _a, "ActiveMember", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash")).Id;
        _suspended = (await AppRoleMembers.AddAsync(_fixture, _a, "SuspendedMember", WorkspaceRole.Member, WorkspaceMemberStatus.Suspended, "hash")).Id;
        _removed = (await AppRoleMembers.AddAsync(_fixture, _a, "RemovedMember", WorkspaceRole.Member, WorkspaceMemberStatus.Removed, "hash")).Id;
        _revoked = (await AppRoleMembers.AddAsync(_fixture, _a, "RevokedMember", WorkspaceRole.Member, WorkspaceMemberStatus.Active, "hash")).Id;
        _invitee = (await AppRoleUsers.AddAsync(_fixture, _a.InvitationEmail, "InviteeUser", "hash")).Id;

        await using var owner = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await owner.OpenAsync();
        await using var revoke = new NpgsqlCommand("UPDATE authz.access_grants SET revoked_at = now() WHERE user_id = @u", owner);
        revoke.Parameters.AddWithValue("u", _revoked);
        (await revoke.ExecuteNonQueryAsync()).Should().BeGreaterThan(0, "the revoked member must hold a grant that is revoked");

        foreach (var table in ClassA)
        {
            foreach (var tenant in new[] { _a, _b })
            {
                await SeedRowAsync(owner, table, tenant);
            }
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// One row of the table in the tenant's Workspace, written as the owner with foreign keys not enforced (the
    /// subject is the policy, not the schema). Required columns get a value of their type; a text column bound by a
    /// check constraint takes the first literal the constraint names.
    /// </summary>
    private static async Task SeedRowAsync(NpgsqlConnection owner, string table, AppRoleTenant tenant)
    {
        var (schema, name) = (table.Split('.')[0], table.Split('.')[1]);
        var columns = new List<(string Name, string Type, string Udt, int? Length)>();
        await using (var q = new NpgsqlCommand(
            "SELECT column_name, data_type, udt_name, character_maximum_length FROM information_schema.columns " +
            "WHERE table_schema = @s AND table_name = @t AND is_generated = 'NEVER' AND is_identity = 'NO' " +
            "AND (column_name IN ('account_id', 'workspace_id') OR (is_nullable = 'NO' AND column_default IS NULL)) " +
            "ORDER BY ordinal_position", owner))
        {
            q.Parameters.AddWithValue("s", schema);
            q.Parameters.AddWithValue("t", name);
            await using var r = await q.ExecuteReaderAsync();
            while (await r.ReadAsync())
                columns.Add((r.GetString(0), r.GetString(1), r.GetString(2), r.IsDBNull(3) ? null : r.GetInt32(3)));
        }

        var checks = new List<string>();
        await using (var q = new NpgsqlCommand(
            "SELECT pg_get_constraintdef(c.oid) FROM pg_constraint c WHERE c.conrelid = @t::regclass AND c.contype = 'c'", owner))
        {
            q.Parameters.AddWithValue("t", table);
            await using var r = await q.ExecuteReaderAsync();
            while (await r.ReadAsync()) checks.Add(r.GetString(0));
        }

        Overrides.TryGetValue(table, out var overrides);
        var values = columns.Select(c =>
        {
            if (overrides is not null && overrides.TryGetValue(c.Name, out var literal)) return literal;
            return c.Name switch
            {
                "account_id" => "@a::uuid",
                "workspace_id" => "@w::uuid",
                _ => ValueFor(c, checks),
            };
        }).ToList();

        var sql = $"INSERT INTO {table} ({string.Join(", ", columns.Select(c => $"\"{c.Name}\""))}) VALUES ({string.Join(", ", values)})";
        await using var tx = await owner.BeginTransactionAsync();
        await using (var replica = new NpgsqlCommand("SET LOCAL session_replication_role = replica", owner, tx))
            await replica.ExecuteNonQueryAsync();
        await using var insert = new NpgsqlCommand(sql, owner, tx);
        insert.Parameters.AddWithValue("a", tenant.AccountId);
        insert.Parameters.AddWithValue("w", tenant.WorkspaceId);
        try
        {
            await insert.ExecuteNonQueryAsync();
        }
        catch (PostgresException ex)
        {
            throw new InvalidOperationException($"Could not seed {table}: {ex.MessageText} (constraint {ex.ConstraintName}). Add an override. SQL: {sql}", ex);
        }

        await tx.CommitAsync();
    }

    private static string ValueFor((string Name, string Type, string Udt, int? Length) c, List<string> checks)
    {
        switch (c.Type)
        {
            case "uuid": return "gen_random_uuid()";
            case "character varying":
            case "text":
            case "character":
            case "citext":
                var fromCheck = checks
                    .Where(d => Regex.IsMatch(d, $@"\b{c.Name}\b"))
                    .Select(d => Regex.Match(d, @"'([^']+)'::"))
                    .FirstOrDefault(m => m.Success);
                if (fromCheck is not null) return $"'{fromCheck.Groups[1].Value}'";
                var len = Math.Min(c.Length ?? 12, 12);
                return $"left(md5(random()::text), {len})";
            case "integer": case "bigint": case "smallint": return "1";
            case "numeric": case "double precision": case "real": return "1";
            case "boolean": return "false";
            case "timestamp with time zone": case "timestamp without time zone": return "now()";
            case "date": return "current_date";
            case "jsonb": case "json": return "'{}'::jsonb";
            case "bytea": return "'\\x00'::bytea";
            case "ARRAY": return $"'{{}}'::{c.Udt.TrimStart('_')}[]";
            case "inet": return "'127.0.0.1'::inet";
            case "interval": return "'1 hour'::interval";
            default: throw new InvalidOperationException($"No generic value for {c.Name} ({c.Type})");
        }
    }

    private enum Scope { App, Token, WorkerOnApp, WorkerLogin }

    private sealed record Caller(string Name, Guid? User, AppRoleTenant Tenant, Scope Scope = Scope.App);

    private IEnumerable<Caller> Callers() =>
    [
        new("active member of A", _activeMember, _a),
        new("owner of A", _a.User.Id, _a),
        new("owner of B", _b.User.Id, _b),
        new("member of another workspace (A's owner in B)", _a.User.Id, _b),
        new("suspended member", _suspended, _a),
        new("removed member", _removed, _a),
        new("revoked member", _revoked, _a),
        new("no user", null, _a),
        new("unknown user", Guid.NewGuid(), _a),
        new("worker scope on the application login", Guid.NewGuid(), _a, Scope.WorkerOnApp),
        new("worker login", null, _a, Scope.WorkerLogin),
        new("invitee scope", _invitee, _a, Scope.Token),
    ];

    private async Task<(NpgsqlConnection Connection, NpgsqlTransaction Transaction)> OpenAsync(Caller caller)
    {
        var connection = new NpgsqlConnection(caller.Scope == Scope.WorkerLogin ? _fixture.WorkerConnectionString : _fixture.AppConnectionString);
        await connection.OpenAsync();
        var transaction = await connection.BeginTransactionAsync();
        var requestScope = caller.Scope is Scope.WorkerOnApp or Scope.WorkerLogin ? "worker" : "app";
        await using (var scope = new NpgsqlCommand(
            "SELECT set_config('app.current_user_id', @u, true), set_config('app.current_account_id', @a, true), " +
            "set_config('app.current_workspace_id', @w, true), set_config('app.request_scope', @s, true)", connection, transaction))
        {
            scope.Parameters.AddWithValue("u", caller.User?.ToString() ?? string.Empty);
            scope.Parameters.AddWithValue("a", caller.Tenant.AccountId.ToString());
            scope.Parameters.AddWithValue("w", caller.Tenant.WorkspaceId.ToString());
            scope.Parameters.AddWithValue("s", requestScope);
            await scope.ExecuteNonQueryAsync();
        }

        if (caller.Scope == Scope.Token)
        {
            await using var enter = new NpgsqlCommand(
                "SELECT count(*) FROM ops.enter_token_scope('workspace-invitation', @h, @v)", connection, transaction);
            enter.Parameters.AddWithValue("h", caller.Tenant.InvitationTokenHash);
            enter.Parameters.AddWithValue("v", caller.Tenant.InvitationHashVersion);
            ((long)(await enter.ExecuteScalarAsync())!).Should().Be(1, "the invitation token scope must open");
            await using var invitee = new NpgsqlCommand("SELECT ops.enter_invitee_scope(@u)", connection, transaction);
            invitee.Parameters.AddWithValue("u", caller.User!.Value);
            ((bool)(await invitee.ExecuteScalarAsync())!).Should().BeTrue("the invitee scope must open");
        }

        return (connection, transaction);
    }

    private static async Task<long> CountAsync(NpgsqlConnection c, NpgsqlTransaction tx, string sql, params (string, object)[] args)
    {
        await using var command = new NpgsqlCommand(sql, c, tx);
        foreach (var (name, value) in args) command.Parameters.AddWithValue(name, value);
        return (long)(await command.ExecuteScalarAsync())!;
    }

    /// <summary>The (account, workspace) pair of every row of the table, read as the owner (which no policy binds).</summary>
    private async Task<(Guid?[] Accounts, Guid?[] Workspaces)> PairsAsync(string table)
    {
        await using var owner = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await owner.OpenAsync();
        await using var q = new NpgsqlCommand($"SELECT account_id, workspace_id FROM {table}", owner);
        await using var r = await q.ExecuteReaderAsync();
        var accounts = new List<Guid?>();
        var workspaces = new List<Guid?>();
        while (await r.ReadAsync())
        {
            accounts.Add(r.IsDBNull(0) ? null : r.GetGuid(0));
            workspaces.Add(r.IsDBNull(1) ? null : r.GetGuid(1));
        }

        return (accounts.ToArray(), workspaces.ToArray());
    }

    [Fact]
    public async Task EveryTableWithThePlainWorkspaceRuleHasTheFastPathAndNoOtherTableDoes()
    {
        await using var owner = new NpgsqlConnection(_fixture.OwnerConnectionString);
        await owner.OpenAsync();
        await using var q = new NpgsqlCommand(
            "SELECT DISTINCT schemaname || '.' || tablename FROM pg_policies WHERE roles::text LIKE '%notrelix_app%' " +
            "AND schemaname <> 'chat' AND (qual LIKE '%accessible_workspace_pairs%' OR with_check LIKE '%accessible_workspace_pairs%')", owner);
        var withFastPath = new List<string>();
        await using (var r = await q.ExecuteReaderAsync())
            while (await r.ReadAsync()) withFastPath.Add(r.GetString(0));

        withFastPath.Should().BeEquivalentTo(ClassA, "031 touches exactly the class A tables, and every one of them was rewritten");

        await using var plain = new NpgsqlCommand(
            "SELECT count(*) FROM pg_policies WHERE roles::text LIKE '%notrelix_app%' AND schemaname || '.' || tablename = ANY (@t) " +
            "AND (qual LIKE '%accessible_workspace_pairs%' OR with_check LIKE '%accessible_workspace_pairs%')", owner);
        plain.Parameters.AddWithValue("t", ClassBWithWorkspaceRule);
        ((long)(await plain.ExecuteScalarAsync())!).Should().Be(0, "a table with anything beyond the plain rule keeps its old policy");

        // Every application policy of a class A table is the fast rule in full: no policy was left half rewritten.
        await using var halves = new NpgsqlCommand(
            "SELECT schemaname || '.' || tablename || '.' || policyname FROM pg_policies WHERE roles::text LIKE '%notrelix_app%' " +
            "AND schemaname || '.' || tablename = ANY (@t) AND policyname IN ('p_app_select','p_app_insert','p_app_update','p_app_delete') " +
            "AND NOT ((qual IS NULL OR qual LIKE '%accessible_workspace_pairs%has_workspace_access%') " +
            "AND (with_check IS NULL OR with_check LIKE '%accessible_workspace_pairs%has_workspace_access%'))", owner);
        halves.Parameters.AddWithValue("t", ClassA);
        var notRewritten = new List<string>();
        await using var hr = await halves.ExecuteReaderAsync();
        while (await hr.ReadAsync()) notRewritten.Add(hr.GetString(0));
        notRewritten.Should().BeEmpty();
    }

    [Fact]
    public async Task ThePolicyAgreesWithTheOriginalFunctionRowForRowOnEveryClassATable()
    {
        var failures = new List<string>();
        var positiveControls = 0;

        foreach (var table in ClassA)
        {
            var (accounts, workspaces) = await PairsAsync(table);
            foreach (var tenant in new[] { _a, _b })
            {
                workspaces.Count(w => w == tenant.WorkspaceId).Should().BeGreaterThan(0, $"{table} must hold a seeded row in workspace {tenant.WorkspaceId}");
            }

            foreach (var caller in Callers())
            {
                var (c, tx) = await OpenAsync(caller);
                await using var _ = c;
                await using var __ = tx;
                try
                {
                    var seen = await CountAsync(c, tx, $"SELECT count(*) FROM {table}");
                    var accepted = await CountAsync(c, tx,
                        "SELECT count(*) FROM unnest(@a::uuid[], @w::uuid[]) v(a, w) WHERE ops.has_workspace_access(v.a, v.w)",
                        ("a", accounts), ("w", workspaces));
                    if (seen != accepted)
                        failures.Add($"{table} as {caller.Name}: policy shows {seen}, function accepts {accepted}");

                    var inA = await CountAsync(c, tx, $"SELECT count(*) FROM {table} WHERE workspace_id = @w", ("w", _a.WorkspaceId));
                    var inB = await CountAsync(c, tx, $"SELECT count(*) FROM {table} WHERE workspace_id = @w", ("w", _b.WorkspaceId));
                    switch (caller.Name)
                    {
                        case "active member of A" or "owner of A" or "invitee scope":
                            if (inA < 1) failures.Add($"{table} as {caller.Name}: reads none of its own workspace's rows");
                            else positiveControls++;
                            if (inB != 0) failures.Add($"{table} as {caller.Name}: reads {inB} rows of workspace B");
                            break;
                        case "member of another workspace (A's owner in B)":
                            if (inB != 0) failures.Add($"{table} as {caller.Name}: reads {inB} rows of workspace B, where it holds no grant");
                            break;
                        case "owner of B":
                            if (inB < 1) failures.Add($"{table} as {caller.Name}: reads none of its own workspace's rows");
                            if (inA != 0) failures.Add($"{table} as {caller.Name}: reads {inA} rows of workspace A");
                            break;
                        case "worker login":
                            if (inA < 1 || inB < 1) failures.Add($"{table} as {caller.Name}: the worker policy must still see both workspaces ({inA}/{inB})");
                            break;
                        default:
                            if (seen != 0) failures.Add($"{table} as {caller.Name}: reads {seen} rows, expected none");
                            break;
                    }
                }
                catch (PostgresException ex)
                {
                    failures.Add($"{table} as {caller.Name}: {ex.MessageText}");
                }
            }
        }

        failures.Should().BeEmpty();
        positiveControls.Should().BeGreaterThan(ClassA.Length, "the positive controls ran for the members and the invitee on every table");
    }
}
