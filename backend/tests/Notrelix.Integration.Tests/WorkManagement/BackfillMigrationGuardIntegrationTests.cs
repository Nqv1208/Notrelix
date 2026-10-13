using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Data.WorkManagement;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Integration;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// The Status-category and Item-key backfills never finish silently incomplete.
/// Run under the application role, row-level security would hide rows, so each
/// backfill fails with its "blocked" error before writing anything; run as the
/// table owner (the required migration identity) each completes and leaves no
/// uncategorized Status option and no keyless Item. Uses a dedicated container
/// with migrations and the RLS policy pack applied.
/// </summary>
public sealed class BackfillMigrationGuardIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 0, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16.15-bookworm")
        .WithDatabase("notrelix_backfill_guard")
        .WithUsername("notrelix")
        .WithPassword("notrelix_test")
        .WithCleanUp(true)
        .Build();

    private Guid _optionId;
    private Guid _itemId;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync();
            await new RlsPolicyApplier(context, NullLogger<RlsPolicyApplier>.Instance).ApplyAsync();
        }

        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var workspace = Workspace.Create(accountId, userId, "Guard WS", $"guard-{Guid.NewGuid():N}", Now);
        var board = Board.Create(accountId, workspace.Id, userId, "Guard Board", null, Now, itemKeyPrefix: "OPS");
        var group = BoardGroup.Create(accountId, workspace.Id, board.Id, "G", Color.Create("#808080"), FractionalIndex.Create("a0"), userId, Now);
        var status = BoardField.Create(accountId, workspace.Id, board.Id, "Status", FieldType.Status,
            FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")), FractionalIndex.Create("a1"), userId, Now);
        status.AddDefaultStatusOptions(userId, Now);
        var item = BoardItem.CreateRoot(accountId, workspace.Id, board.Id, group.Id, "Legacy", FractionalIndex.Create("a0"), userId, Now);
        (_optionId, _itemId) = (status.Options.First().Id, item.Id);

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using (var seed = CreateContext(tenant))
        {
            seed.Workspaces.Add(workspace);
            seed.Boards.Add(board);
            seed.BoardGroups.Add(group);
            seed.BoardFields.Add(status);
            seed.BoardItems.Add(item);
            await seed.SaveChangesAsync();
        }

        // Legacy rows: one Status option without a category, one Item without a key.
        await ExecuteAsync($"UPDATE work.field_options SET category = NULL WHERE id = '{_optionId}'");
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    [Fact]
    public async Task UnderTheApplicationRole_BothBackfillsFailBlocked_AndWriteNothing()
    {
        var category = () => ExecuteAsAppRoleAsync(StatusOptionCategoryMigrationSql.Backfill);
        var keys = () => ExecuteAsAppRoleAsync(ItemKeyMigrationSql.Backfill);

        (await category.Should().ThrowAsync<PostgresException>()).Which.MessageText.Should().StartWith(StatusOptionCategoryMigrationSql.BlockedPrefix);
        (await keys.Should().ThrowAsync<PostgresException>()).Which.MessageText.Should().StartWith(ItemKeyMigrationSql.BlockedPrefix);
        (await ScalarAsync($"SELECT coalesce(category, '<null>') FROM work.field_options WHERE id = '{_optionId}'")).Should().Be("<null>");
        (await ScalarAsync($"SELECT coalesce(item_key, '<null>') FROM work.board_items WHERE id = '{_itemId}'")).Should().Be("<null>");
    }

    [Fact]
    public async Task AsTheTableOwner_BothBackfillsComplete()
    {
        await ExecuteAsync(StatusOptionCategoryMigrationSql.Backfill);
        await ExecuteAsync(ItemKeyMigrationSql.Backfill);

        (await ScalarAsync($"SELECT category FROM work.field_options WHERE id = '{_optionId}'")).Should().NotBeNull();
        (await ScalarAsync($"SELECT item_key FROM work.board_items WHERE id = '{_itemId}'")).Should().Be("OPS-1");
        (await ScalarAsync("SELECT count(*)::text FROM work.board_items WHERE item_key IS NULL")).Should().Be("0");
    }

    private Task ExecuteAsAppRoleAsync(string sql) => ExecuteAsync($"""
        BEGIN;
        SET LOCAL ROLE notrelix_app;
        {sql}
        COMMIT;
        """);

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
}
