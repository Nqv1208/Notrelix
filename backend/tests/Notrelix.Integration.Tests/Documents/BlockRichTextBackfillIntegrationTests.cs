using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Documents;
using Notrelix.Testing.Integration;

namespace Notrelix.Integration.Tests.Documents;

/// <summary>
/// A database at the schema before rich text v1 holds blocks in the flat legacy shape; the real migration
/// upgrades them (marks over the whole text, properties by type, the previous shape kept in
/// <c>legacy_content</c>), leaves rows that are already v1 alone, and running the backfill again changes
/// nothing. Uses a dedicated container because the shared one is already at the latest schema.
/// </summary>
public sealed class BlockRichTextBackfillIntegrationTests : IAsyncLifetime
{
    private const string MigrationBefore = "20261025165049_AddIntegrationConnectionDeliveryState";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16.15-bookworm")
        .WithDatabase("notrelix_block_rich_text")
        .WithUsername("notrelix")
        .WithPassword("notrelix_test")
        .WithCleanUp(true)
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.GetService<IMigrator>().MigrateAsync(MigrationBefore);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    [Fact]
    public async Task Migration_UpgradesLegacyBlocks_KeepsTheOldShape_AndARerunChangesNothing()
    {
        var seed = await InsertAsync(
            ("Text", "\"Content block 1 for page 1\"", "{}"),
            ("Heading1", "{\"text\":\"Title\",\"bold\":true,\"textColor\":\"destructive\",\"align\":\"center\"}", "{}"),
            ("ToDo", "{\"text\":\"Ship\",\"checked\":true}", "{}"),
            ("Code", "{\"text\":\"let x\",\"language\":\"TypeScript\",\"bold\":true}", "{}"),
            ("PageReference", "{\"linkedPageId\":\"0194c3a0-aaaa-7000-8000-000000000001\"}", "{}"),
            ("Table", "{\"rows\":[[\"a\",\"b\"]]}", "{}"),
            ("Text", "{\"v\":1,\"runs\":[{\"text\":\"already\",\"marks\":[]}]}", "{\"align\":\"left\"}"),
            ("Divider", "{}", "{}"));

        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync();
        }

        (await ContentAsync(seed[0])).Should().Be("{\"v\": 1, \"runs\": [{\"text\": \"Content block 1 for page 1\", \"marks\": []}]}");
        (await ContentAsync(seed[1])).Should().Be("{\"v\": 1, \"runs\": [{\"text\": \"Title\", \"marks\": [{\"type\": \"bold\"}, {\"type\": \"color\", \"token\": \"danger\"}]}]}");
        (await PropertiesAsync(seed[1])).Should().Be("{\"align\": \"center\"}");
        (await PropertiesAsync(seed[2])).Should().Be("{\"checked\": true}");
        (await ContentAsync(seed[3])).Should().Be("{\"v\": 1, \"runs\": [{\"text\": \"let x\", \"marks\": []}]}");
        (await PropertiesAsync(seed[3])).Should().Be("{\"language\": \"typescript\"}");
        (await ContentAsync(seed[4])).Should().Be("{}");
        (await PropertiesAsync(seed[4])).Should().Be("{\"pageId\": \"0194c3a0-aaaa-7000-8000-000000000001\"}");
        (await PropertiesAsync(seed[5])).Should().Contain("\"rows\"");
        (await LegacyAsync(seed[1])).Should().Contain("\"bold\": true");
        (await LegacyAsync(seed[6])).Should().BeNull("a block that already is rich text v1 is not rewritten");
        (await VersionAsync(seed[6])).Should().Be(1);
        (await VersionAsync(seed[7])).Should().Be(1);
        (await VersionAsync(seed[0])).Should().Be(2, "a rewritten block moves its version forward");

        var affected = await ExecuteAsync(BlockRichTextMigrationSql.CreateUpgradeFunction + BlockRichTextMigrationSql.Backfill);
        affected.Should().BeGreaterThanOrEqualTo(0);
        (await VersionAsync(seed[0])).Should().Be(2, "a second run rewrites nothing");
    }

    private async Task<List<Guid>> InsertAsync(params (string Type, string Content, string Properties)[] rows)
    {
        var ids = new List<Guid>();
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using (var relax = new NpgsqlCommand("SET session_replication_role = replica", connection))
            await relax.ExecuteNonQueryAsync();

        var position = 0;
        foreach (var (type, content, properties) in rows)
        {
            var id = Guid.NewGuid();
            ids.Add(id);
            await using var command = new NpgsqlCommand(
                "INSERT INTO docs.blocks (id, account_id, workspace_id, page_id, type, content, properties, position, created_at, version) " +
                "VALUES (@id, @a, @w, @p, @type, @content::jsonb, @props::jsonb, @pos, now(), 1)", connection);
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("a", Guid.NewGuid());
            command.Parameters.AddWithValue("w", Guid.NewGuid());
            command.Parameters.AddWithValue("p", Guid.NewGuid());
            command.Parameters.AddWithValue("type", type);
            command.Parameters.AddWithValue("content", content);
            command.Parameters.AddWithValue("props", properties);
            command.Parameters.AddWithValue("pos", $"a{position++}");
            await command.ExecuteNonQueryAsync();
        }

        return ids;
    }

    private Task<string?> ContentAsync(Guid id) => ScalarAsync($"SELECT content::text FROM docs.blocks WHERE id = '{id}'");
    private Task<string?> PropertiesAsync(Guid id) => ScalarAsync($"SELECT properties::text FROM docs.blocks WHERE id = '{id}'");
    private Task<string?> LegacyAsync(Guid id) => ScalarAsync($"SELECT legacy_content::text FROM docs.blocks WHERE id = '{id}'");
    private async Task<long> VersionAsync(Guid id) => long.Parse((await ScalarAsync($"SELECT version::text FROM docs.blocks WHERE id = '{id}'"))!);

    private ApplicationDbContext CreateContext()
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

        return new ApplicationDbContext(options);
    }

    private async Task<int> ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteNonQueryAsync();
    }

    private async Task<string?> ScalarAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteScalarAsync() as string;
    }
}
