using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Checklists;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Integration;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// RLS-OPTION-001 / RLS-VALUE-001 / RLS-CHK-001 / RLS-WORK-ALL-001 / RLS-VERIFY-001
/// (WM-V3-DATA-003). The six `work` tables without tenant columns must derive their
/// tenant scope from their parent under the application role, and the policy
/// verification must report no RLS-disabled or policyless `work` table.
///
/// Uses a dedicated container with migrations AND the RLS policy pack applied.
/// Rows are seeded as the superuser for two tenants; access is probed as
/// `notrelix_app` for a user granted only tenant A.
/// </summary>
public sealed class WorkChildTableRlsIntegrationTests : IAsyncLifetime
{
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-0000000000C1");
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 0, 0, 0, TimeSpan.Zero);

    private static readonly string[] ChildTables =
    [
        "field_options", "checklist_items", "board_item_values", "board_members", "approval_steps", "relation_field_configs",
    ];

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16.15-bookworm")
        .WithDatabase("notrelix_rls_children")
        .WithUsername("notrelix")
        .WithPassword("notrelix_test")
        .WithCleanUp(true)
        .Build();

    private Tenant _a = null!;
    private Tenant _b = null!;

    private sealed record Tenant(Guid AccountId, Guid WorkspaceId, Guid BoardId, Guid FieldId, Guid ItemId, Guid ChecklistId);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync();
            await new RlsPolicyApplier(context, NullLogger<RlsPolicyApplier>.Instance).ApplyAsync();
        }

        _a = await SeedTenantAsync();
        _b = await SeedTenantAsync();
        await SeedGrantAsync(_a);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    [Fact]
    public async Task AppRole_SeesOnlyChildRowsOfItsOwnTenant()
    {
        foreach (var table in ChildTables)
        {
            var visible = await QueryAsAppRoleAsync($"SELECT id::text FROM work.{table}");

            visible.Should().ContainSingle(
                $"work.{table} must derive tenant scope from its parent; the user is granted tenant A only")
                .Which.Should().Be(ChildId(_a, table).ToString());
        }
    }

    [Fact]
    public async Task AppRole_CannotInsertFieldOptionUnderForeignField()
    {
        var act = () => QueryAsAppRoleAsync($"""
            INSERT INTO work.field_options (id, field_id, name, color, position)
            VALUES ('{Guid.NewGuid()}', '{_b.FieldId}', 'smuggled', '#808080', 'a5') RETURNING id::text
            """);

        (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be("42501");
    }

    [Fact]
    public async Task AppRole_CannotWriteItemValueAcrossBoards()
    {
        var act = () => QueryAsAppRoleAsync($"""
            INSERT INTO work.board_item_values (id, item_id, field_id, value)
            VALUES ('{Guid.NewGuid()}', '{_a.ItemId}', '{_b.FieldId}', '""'::jsonb) RETURNING id::text
            """);

        (await act.Should().ThrowAsync<PostgresException>(
            "a value must reference an item and a field of the same current-tenant board"))
            .Which.SqlState.Should().Be("42501");
    }

    [Fact]
    public async Task RlsVerification_ReportsNoRlsDisabledOrPolicylessWorkTable()
    {
        var issues = await QueryAsSuperuserAsync("""
            SELECT c.relname || ':' || CASE WHEN c.relrowsecurity THEN 'NO_POLICY' ELSE 'RLS_DISABLED' END
            FROM pg_class c
            JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE c.relkind = 'r' AND n.nspname = 'work'
              AND (c.relrowsecurity = false
                   OR NOT EXISTS (SELECT 1 FROM pg_policies p WHERE p.schemaname = n.nspname AND p.tablename = c.relname))
            ORDER BY 1
            """);

        issues.Should().BeEmpty("every work table must have RLS enabled with at least one policy (WMREQ080, WMREQ093)");
    }

    [Fact]
    public async Task RlsPolicyApplier_FailsClosed_WhenAWorkTableHasNoRls()
    {
        await QueryAsSuperuserAsync("CREATE TABLE work.rls_verification_probe (id uuid PRIMARY KEY); SELECT 'created'");
        try
        {
            await using var context = CreateContext();
            var act = () => new RlsPolicyApplier(context, NullLogger<RlsPolicyApplier>.Instance).ApplyAsync();

            (await act.Should().ThrowAsync<InvalidOperationException>())
                .Which.Message.Should().Contain("work.rls_verification_probe: RLS_DISABLED");
        }
        finally
        {
            await QueryAsSuperuserAsync("DROP TABLE work.rls_verification_probe; SELECT 'dropped'");
        }
    }

    private static Guid ChildId(Tenant tenant, string table) =>
        new(tenant.AccountId.ToByteArray().Take(12).Concat(BitConverter.GetBytes(Array.IndexOf(ChildTables, table))).ToArray());

    private async Task<Tenant> SeedTenantAsync()
    {
        var accountId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var board = Board.Create(accountId, workspaceId, UserId, "RLS Board", null, Now);
        var group = BoardGroup.Create(accountId, workspaceId, board.Id, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), UserId, Now);
        var field = BoardField.Create(accountId, workspaceId, board.Id, "Notes", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"), UserId, Now);
        var item = BoardItem.CreateRoot(accountId, workspaceId, board.Id, group.Id, "Task", FractionalIndex.Create("a0"), UserId, Now);
        var checklist = Checklist.Create(accountId, workspaceId, item.Id, "Checklist", FractionalIndex.Create("a0"), UserId, Now);

        var tenantContext = new FakeCurrentTenantContext();
        tenantContext.SetSystem();
        await using (var context = CreateContext(tenantContext))
        {
            context.Boards.Add(board);
            context.BoardGroups.Add(group);
            context.BoardFields.Add(field);
            context.BoardItems.Add(item);
            context.Checklists.Add(checklist);
            await context.SaveChangesAsync();
        }

        var tenant = new Tenant(accountId, workspaceId, board.Id, field.Id, item.Id, checklist.Id);
        var approvalId = Guid.NewGuid();
        await QueryAsSuperuserAsync($"""
            INSERT INTO work.field_options (id, field_id, name, color, position)
                VALUES ('{ChildId(tenant, "field_options")}', '{field.Id}', 'Option', '#808080', 'a0');
            INSERT INTO work.checklist_items (id, checklist_id, title, position, status)
                VALUES ('{ChildId(tenant, "checklist_items")}', '{checklist.Id}', 'Step', 'a0', 'Open');
            INSERT INTO work.board_item_values (id, item_id, field_id, value)
                VALUES ('{ChildId(tenant, "board_item_values")}', '{item.Id}', '{field.Id}', '""'::jsonb);
            INSERT INTO work.board_members (id, board_id, user_id, role, joined_at)
                VALUES ('{ChildId(tenant, "board_members")}', '{board.Id}', '{UserId}', 'Member', now());
            INSERT INTO work.approval_requests (id, account_id, workspace_id, title, status, requested_by_user_id, created_at, version, target_type, target_id)
                VALUES ('{approvalId}', '{accountId}', '{workspaceId}', 'Approval', 'Pending', '{UserId}', now(), 1, 'work-management.board-item', '{item.Id}');
            INSERT INTO work.approval_steps (id, approval_request_id, position, status)
                VALUES ('{ChildId(tenant, "approval_steps")}', '{approvalId}', 0, 'Pending');
            INSERT INTO work.relation_field_configs (id, field_id, source_board_id, target_board_id, direction, allow_multiple, create_backlink)
                VALUES ('{ChildId(tenant, "relation_field_configs")}', '{field.Id}', '{board.Id}', '{board.Id}', 'Outbound', false, false);
            SELECT 'seeded'
            """);

        return tenant;
    }

    private async Task SeedGrantAsync(Tenant tenant)
    {
        await using var context = CreateContext();
        context.AccessGrants.Add(new AccessGrant(
            accountId: tenant.AccountId,
            workspaceId: tenant.WorkspaceId,
            userId: UserId,
            sourceContext: "Workspace",
            membershipStatus: "Active",
            roleCodes: [],
            permissionCodes: [],
            isAccountAdmin: false,
            isWorkspaceAdmin: false,
            grantedAt: Now));
        await context.SaveChangesAsync();
    }

    private ApplicationDbContext CreateContext(ICurrentTenantContext? tenant = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_container.GetConnectionString(), npgOptions =>
            {
                npgOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgOptions.MigrationsHistoryTable("__EFMigrationsHistory", "ops");
            })
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .UseSnakeCaseNamingConvention()
            .ReplaceService<IModelCacheKeyFactory, WorkspaceAwareModelCacheKeyFactory>()
            .Options;

        return tenant is not null ? new ApplicationDbContext(options, tenant) : new ApplicationDbContext(options);
    }

    /// <summary>
    /// Runs SQL as notrelix_app with the production session context, inside one
    /// explicit transaction so the transaction-local settings apply.
    /// </summary>
    private Task<List<string>> QueryAsAppRoleAsync(string sql) => ExecuteAsync($"""
        BEGIN;
        SET ROLE notrelix_app;
        DO $$
        BEGIN
            PERFORM set_config('app.current_user_id', '{UserId}', true);
            PERFORM set_config('app.request_scope', 'app', true);
        END
        $$;
        {sql};
        COMMIT;
        """);

    private Task<List<string>> QueryAsSuperuserAsync(string sql) => ExecuteAsync(sql);

    private async Task<List<string>> ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var values = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        do
        {
            while (await reader.ReadAsync())
            {
                values.Add(reader.GetString(0));
            }
        }
        while (await reader.NextResultAsync());

        return values;
    }
}
