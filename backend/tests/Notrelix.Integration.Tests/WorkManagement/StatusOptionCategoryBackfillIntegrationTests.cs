using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.WorkManagement;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Integration;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// A database at the schema before Status options had a category is upgraded
/// through the real migration chain: existing Status options are categorized by
/// name (case- and diacritic-insensitive) and position, options of other field
/// types stay uncategorized, and running the backfill again changes nothing. Uses a
/// dedicated container because the shared one is already at the latest schema.
/// </summary>
public sealed class StatusOptionCategoryBackfillIntegrationTests : IAsyncLifetime
{
    private const string MigrationBefore = "20261011080924_DropCrossContextSpaceForeignKeys";
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 0, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16.15-bookworm")
        .WithDatabase("notrelix_status_category")
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
    public async Task Migration_CategorizesExistingStatusOptions_AndARerunChangesNothing()
    {
        var (english, vietnamese, mixed, select) = await SeedFieldsAsync();
        await InsertOptionsAsync(english, ("To Do", "a0"), ("Working on it", "a1"), ("Stuck", "a2"), ("Done", "a3"));
        await InsertOptionsAsync(vietnamese, ("Cần làm", "a0"), ("Đang làm", "a1"), ("Chờ duyệt", "a2"), ("Hoàn thành", "a3"));
        await InsertOptionsAsync(mixed, ("  HOAN   thanh ", "a0"), ("In Review", "a1"), ("Backlog", "a2"), ("ĐANG DUYỆT", "a3"), ("Xong", "a4"));
        await InsertOptionsAsync(select, ("Low", "a0"), ("Done", "a1"));

        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync();
        }

        (await CategoriesAsync(english)).Should().Equal(
            "To Do=Todo", "Working on it=InProgress", "Stuck=InProgress", "Done=Done");
        (await CategoriesAsync(vietnamese)).Should().Equal(
            "Cần làm=Todo", "Đang làm=InProgress", "Chờ duyệt=Review", "Hoàn thành=Done");
        (await CategoriesAsync(mixed)).Should().Equal(
            new[] { "  HOAN   thanh =Done", "In Review=Review", "Backlog=Todo", "ĐANG DUYỆT=Review", "Xong=Done" },
            "name rules win over position; the lowest remaining option is the Todo");
        (await CategoriesAsync(select)).Should().Equal(
            new[] { "Low=<null>", "Done=<null>" }, "only Status options carry a category");

        var before = await AllCategoriesAsync();
        var affected = await ExecuteAsync(StatusOptionCategoryMigrationSql.Backfill);

        affected.Should().Be(0, "every Status option is already categorized");
        (await AllCategoriesAsync()).Should().Equal(before);
    }

    private async Task<(Guid English, Guid Vietnamese, Guid Mixed, Guid Select)> SeedFieldsAsync()
    {
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var workspace = Workspace.Create(accountId, userId, "Category WS", $"cat-{Guid.NewGuid():N}", Now);
        var board = Board.Create(accountId, workspace.Id, userId, "Category Board", null, Now);
        var status = FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}"));
        var english = BoardField.Create(accountId, workspace.Id, board.Id, "Status", FieldType.Status, status, FractionalIndex.Create("a0"), userId, Now);
        var vietnamese = BoardField.Create(accountId, workspace.Id, board.Id, "Trạng thái", FieldType.Status, status, FractionalIndex.Create("a1"), userId, Now);
        var mixed = BoardField.Create(accountId, workspace.Id, board.Id, "Stage", FieldType.Status, status, FractionalIndex.Create("a2"), userId, Now);
        var select = BoardField.Create(accountId, workspace.Id, board.Id, "Priority", FieldType.Select, FieldSettings.Empty(), FractionalIndex.Create("a3"), userId, Now);

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);
        await LegacySchemaWorkspaceSeed.InsertAsync(context, workspace);
        context.Boards.Add(board);
        context.BoardFields.AddRange(english, vietnamese, mixed, select);
        await context.SaveChangesAsync();
        return (english.Id, vietnamese.Id, mixed.Id, select.Id);
    }

    private async Task InsertOptionsAsync(Guid fieldId, params (string Name, string Position)[] options)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        foreach (var (name, position) in options)
        {
            await using var command = new NpgsqlCommand(
                "INSERT INTO work.field_options (id, field_id, name, color, position) VALUES (@id, @field, @name, '#808080', @position)",
                connection);
            command.Parameters.AddWithValue("id", Guid.NewGuid());
            command.Parameters.AddWithValue("field", fieldId);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("position", position);
            await command.ExecuteNonQueryAsync();
        }
    }

    private Task<List<string>> CategoriesAsync(Guid fieldId) => QueryAsync(
        $"SELECT name || '=' || coalesce(category, '<null>') FROM work.field_options WHERE field_id = '{fieldId}' ORDER BY position COLLATE \"C\"");

    private Task<List<string>> AllCategoriesAsync() => QueryAsync(
        "SELECT id::text || '=' || coalesce(category, '<null>') FROM work.field_options ORDER BY id");

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
