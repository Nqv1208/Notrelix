using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.WorkManagement;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Integration;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// A database at the schema before Item keys were backfilled is upgraded through
/// the real migration chain: Items without a key get the next numbers of their
/// Board sequence in creation order (created_at, then id; deleted Items included),
/// prefixed or plain, numbering continues after an Item that already had a key,
/// each Board's sequence is raised to its highest number, and running the backfill
/// again changes nothing. Uses a dedicated container because the shared one is
/// already at the latest schema.
/// </summary>
public sealed class ItemKeyBackfillIntegrationTests : IAsyncLifetime
{
    private const string MigrationBefore = "20261012160353_AddStatusOptionCategory";
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 0, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16.15-bookworm")
        .WithDatabase("notrelix_item_keys")
        .WithUsername("notrelix")
        .WithPassword("notrelix_test")
        .WithCleanUp(true)
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.GetService<IMigrator>().MigrateAsync(MigrationBefore);
        await LegacySchemaWorkspaceSeed.AddLaterItemColumnsAsync(context);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    [Fact]
    public async Task Migration_KeysExistingItemsInCreationOrder_SetsTheSequence_AndARerunChangesNothing()
    {
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var workspace = Workspace.Create(accountId, userId, "Keys WS", $"keys-{Guid.NewGuid():N}", Now);
        var prefixed = Board.Create(accountId, workspace.Id, userId, "Prefixed", null, Now, itemKeyPrefix: "WEB");
        var plain = Board.Create(accountId, workspace.Id, userId, "Plain", null, Now);
        var prefixedGroup = BoardGroup.Create(accountId, workspace.Id, prefixed.Id, "G", Color.Create("#808080"), FractionalIndex.Create("a0"), userId, Now);
        var plainGroup = BoardGroup.Create(accountId, workspace.Id, plain.Id, "G", Color.Create("#808080"), FractionalIndex.Create("a0"), userId, Now);

        // Created out of position order; creation time decides the numbers.
        var third = Item(prefixed, prefixedGroup, "third", "a0", Now.AddMinutes(3));
        var first = Item(prefixed, prefixedGroup, "first", "a1", Now.AddMinutes(1));
        var second = Item(prefixed, prefixedGroup, "second (deleted)", "a2", Now.AddMinutes(2));
        second.Delete(userId, Now.AddMinutes(10));
        var plainA = Item(plain, plainGroup, "plain a", "a0", Now.AddMinutes(1));
        var plainKeyed = Item(plain, plainGroup, "already keyed", "a1", Now.AddMinutes(2), key: "5", sequence: 5);
        var plainB = Item(plain, plainGroup, "plain b", "a2", Now.AddMinutes(3));

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using (var seed = CreateContext(tenant))
        {
            await LegacySchemaWorkspaceSeed.InsertAsync(seed, workspace);
            seed.Boards.AddRange(prefixed, plain);
            seed.BoardGroups.AddRange(prefixedGroup, plainGroup);
            seed.BoardItems.AddRange(third, first, second, plainA, plainKeyed, plainB);
            await seed.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            await LegacySchemaWorkspaceSeed.DropLaterItemColumnsAsync(context);
            await context.Database.MigrateAsync();
        }

        (await KeysAsync(prefixed.Id)).Should().Equal("first=WEB-1/1", "second (deleted)=WEB-2/2", "third=WEB-3/3");
        (await KeysAsync(plain.Id)).Should().Equal(
            new[] { "already keyed=5/5", "plain a=6/6", "plain b=7/7" },
            "numbering continues after the highest number already used on the board");
        (await QueryAsync($"SELECT item_sequence::text FROM work.boards WHERE id = '{prefixed.Id}'")).Should().Equal("3");
        (await QueryAsync($"SELECT item_sequence::text FROM work.boards WHERE id = '{plain.Id}'")).Should().Equal("7");
        (await QueryAsync("SELECT count(*)::text FROM pg_indexes WHERE indexname = 'ux_work_board_items_board_item_key'")).Should().Equal("1");

        var affected = await ExecuteAsync(ItemKeyMigrationSql.Backfill);

        affected.Should().Be(0, "every item already has a key and every board sequence is current");
    }

    private static BoardItem Item(Board board, BoardGroup group, string name, string position, DateTimeOffset createdAt, string? key = null, long? sequence = null) =>
        BoardItem.CreateRoot(board.AccountId, board.WorkspaceId, board.Id, group.Id, name, FractionalIndex.Create(position),
            board.CreatedBy ?? Guid.NewGuid(), createdAt, itemKey: key, itemSequence: sequence);

    private Task<List<string>> KeysAsync(Guid boardId) => QueryAsync(
        $"SELECT name || '=' || coalesce(item_key, '<null>') || '/' || coalesce(item_sequence::text, '<null>') FROM work.board_items WHERE board_id = '{boardId}' ORDER BY item_sequence, name");

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

    private async Task<int> ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteNonQueryAsync();
    }

    private async Task<List<string>> QueryAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<string>();
        while (await reader.ReadAsync())
            rows.Add(reader.GetString(0));
        return rows;
    }
}
