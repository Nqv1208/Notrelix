using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.WorkManagement;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Integration;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// MIG-ORDER-001 (WM-V3-MIG-ORDER-001, SPEC WMREQ100/102). A database migrated to
/// the schema before the ordering cut-over is upgraded through the real migration
/// chain: a duplicate active position aborts the migration with the named
/// preflight error and leaves the schema and data unchanged; once the scope is
/// clean the same migration applies without reordering anything. Uses a dedicated
/// container because the shared one is already at the latest schema.
/// </summary>
public sealed class WorkOrderingPreflightIntegrationTests : IAsyncLifetime
{
    private const string MigrationBeforeCutOver = "20261010022529_WidenBoardDescription";
    private const string CutOverMigration = "20261010065641_CanonicalizeWorkOrdering";
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16.15-bookworm")
        .WithDatabase("notrelix_ordering_preflight")
        .WithUsername("notrelix")
        .WithPassword("notrelix_test")
        .WithCleanUp(true)
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.GetService<IMigrator>().MigrateAsync(MigrationBeforeCutOver);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    [Fact]
    public async Task DuplicateScope_AbortsTheMigration_WithTheNamedError_AndChangesNothing_ThenACleanScopeUpgrades()
    {
        await SeedDuplicateGroupsAsync();

        var act = async () =>
        {
            await using var context = CreateContext();
            await context.Database.MigrateAsync();
        };

        (await act.Should().ThrowAsync<PostgresException>())
            .Which.MessageText.Should().StartWith(WorkOrderingPreflight.ErrorPrefix).And.Contain("work.board_groups");
        (await ScalarAsync("SELECT data_type FROM information_schema.columns WHERE table_schema = 'work' AND table_name = 'board_groups' AND column_name = 'position'"))
            .Should().Be("character varying", "the aborted migration leaves the column untouched");
        (await ScalarAsync($"SELECT count(*)::text FROM pg_indexes WHERE indexname = '{WorkOrderingConstraints.BoardGroups}'")).Should().Be("0");
        (await ScalarAsync($"SELECT count(*)::text FROM ops.\"__EFMigrationsHistory\" WHERE migration_id = '{CutOverMigration}'")).Should().Be("0");
        (await ScalarAsync("SELECT count(*)::text FROM work.board_groups WHERE position = 'a5'")).Should().Be("2", "the preflight never rewrites data");

        await ExecuteAsync("UPDATE work.board_groups SET position = 'a6' WHERE title = 'Second'");
        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync();
        }

        (await ScalarAsync($"SELECT count(*)::text FROM ops.\"__EFMigrationsHistory\" WHERE migration_id = '{CutOverMigration}'")).Should().Be("1");
        (await ScalarAsync("SELECT string_agg(title || '=' || position, ',' ORDER BY position) FROM work.board_groups"))
            .Should().Be("First=a5,Second=a6", "a clean scope upgrades with stable keys and order");
    }

    private async Task SeedDuplicateGroupsAsync()
    {
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var workspace = Workspace.Create(accountId, userId, "Preflight WS", $"pre-{Guid.NewGuid():N}", Now);
        var board = Board.Create(accountId, workspace.Id, userId, "Preflight Board", null, Now);
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);
        context.Workspaces.Add(workspace);
        context.Boards.Add(board);
        foreach (var title in new[] { "First", "Second" })
        {
            context.BoardGroups.Add(BoardGroup.Create(
                accountId, workspace.Id, board.Id, title, Color.Create("#808080"), FractionalIndex.Create("a5"), userId, Now));
        }

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

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<string?> ScalarAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (await command.ExecuteScalarAsync())?.ToString();
    }
}
