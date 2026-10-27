using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
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
using Notrelix.Infrastructure.Data.WorkManagement;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Integration;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// A database whose Status options already have categories is upgraded through
/// the real migration chain: a legacy Status field without a done option gets
/// one (its highest non-to-do option), Items already holding a done status are
/// completed at their last update time, earlier completions and non-done Items
/// are kept, a one-option field is left as it is, and running the backfill again
/// changes nothing. Uses a dedicated container because the shared one is already
/// at the latest schema.
/// </summary>
public sealed class StatusCompletionBackfillIntegrationTests : IAsyncLifetime
{
    private const string MigrationBefore = "20261012164722_AddBoardItemParentIndex";
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 0, 0, 0, TimeSpan.Zero);

    private int _positions;

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16.15-bookworm")
        .WithDatabase("notrelix_status_completion")
        .WithUsername("notrelix")
        .WithPassword("notrelix_test")
        .WithCleanUp(true)
        .Build();

    private IReadOnlyList<string> _laterItemColumns = [];

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.GetService<IMigrator>().MigrateAsync(MigrationBefore);
        _laterItemColumns = await LegacySchemaWorkspaceSeed.AddLaterItemColumnsAsync(context);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    [Fact]
    public async Task Migration_GivesLegacyFieldsADoneOption_CompletesDoneItems_AndARerunChangesNothing()
    {
        var seed = await SeedBoardsAsync();
        var legacy = await InsertOptionsAsync(seed.LegacyField, ("Mở", "a0", "Todo"), ("Đang xử lý", "a1", "InProgress"), ("Đóng", "a2", "InProgress"));
        var modern = await InsertOptionsAsync(seed.ModernField, ("Cần làm", "a0", "Todo"), ("Đang làm", "a1", "InProgress"), ("Hoàn thành", "a2", "Done"));
        var solo = await InsertOptionsAsync(seed.SoloField, ("Mở", "a0", "Todo"));

        var closedItem = await InsertItemAsync(seed.LegacyBoard, seed.LegacyGroup, "Closed", seed.LegacyField, legacy[2]);
        var openItem = await InsertItemAsync(seed.LegacyBoard, seed.LegacyGroup, "Open", seed.LegacyField, legacy[0]);
        var doneItem = await InsertItemAsync(seed.ModernBoard, seed.ModernGroup, "Done", seed.ModernField, modern[2]);
        var workingItem = await InsertItemAsync(seed.ModernBoard, seed.ModernGroup, "Working", seed.ModernField, modern[1]);
        var completedEarlier = await InsertItemAsync(seed.ModernBoard, seed.ModernGroup, "Completed earlier", seed.ModernField, modern[2],
            completedAt: new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero));
        var deletedDone = await InsertItemAsync(seed.ModernBoard, seed.ModernGroup, "Deleted", seed.ModernField, modern[2], deleted: true);
        var soloItem = await InsertItemAsync(seed.SoloBoard, seed.SoloGroup, "Solo", seed.SoloField, solo[0]);

        await using (var context = CreateContext())
        {
            await LegacySchemaWorkspaceSeed.DropLaterItemColumnsAsync(context, _laterItemColumns);
            await context.Database.MigrateAsync();
        }

        (await CategoriesAsync(seed.LegacyField)).Should().Equal(
            new[] { "Mở=Todo", "Đang xử lý=InProgress", "Đóng=Done" }, "the highest non-to-do option becomes the done option");
        (await CategoriesAsync(seed.ModernField)).Should().Equal("Cần làm=Todo", "Đang làm=InProgress", "Hoàn thành=Done");
        (await CategoriesAsync(seed.SoloField)).Should().Equal(new[] { "Mở=Todo" }, "a one-option field cannot hold both a to-do and a done option");

        var completion = await CompletionAsync();
        completion[closedItem].Should().Be(Now.AddHours(2), "completed at its last update time");
        completion[doneItem].Should().Be(Now.AddHours(2));
        completion[completedEarlier].Should().Be(new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero), "an existing completion is kept");
        completion[openItem].Should().BeNull();
        completion[workingItem].Should().BeNull();
        completion[deletedDone].Should().BeNull("deleted Items are not changed");
        completion[soloItem].Should().BeNull();

        var categoriesBefore = await QueryAsync("SELECT id::text || '=' || coalesce(category, '<null>') FROM work.field_options ORDER BY id");
        var affected = await ExecuteAsync(StatusCompletionMigrationSql.Backfill);

        affected.Should().Be(0, "every eligible field has a done option and every done Item is completed");
        (await QueryAsync("SELECT id::text || '=' || coalesce(category, '<null>') FROM work.field_options ORDER BY id")).Should().Equal(categoriesBefore);
        (await CompletionAsync()).Should().BeEquivalentTo(completion);
    }

    private sealed record Seeded(
        Guid LegacyBoard, Guid LegacyGroup, Guid LegacyField,
        Guid ModernBoard, Guid ModernGroup, Guid ModernField,
        Guid SoloBoard, Guid SoloGroup, Guid SoloField);

    private async Task<Seeded> SeedBoardsAsync()
    {
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var workspace = Workspace.Create(accountId, userId, "Completion WS", $"done-{Guid.NewGuid():N}", Now);
        var settings = FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}"));

        (Board Board, BoardGroup Group, BoardField Field) Make(string title)
        {
            var board = Board.Create(accountId, workspace.Id, userId, title, null, Now);
            var group = BoardGroup.Create(accountId, workspace.Id, board.Id, "Group", Color.Create("#0079BF"), FractionalIndex.Create("a0"), userId, Now);
            var field = BoardField.Create(accountId, workspace.Id, board.Id, "Status", FieldType.Status, settings, FractionalIndex.Create("a0"), userId, Now);
            return (board, group, field);
        }

        var legacy = Make("Legacy");
        var modern = Make("Modern");
        var solo = Make("Solo");

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);
        await LegacySchemaWorkspaceSeed.InsertAsync(context, workspace);
        context.Boards.AddRange(legacy.Board, modern.Board, solo.Board);
        context.BoardGroups.AddRange(legacy.Group, modern.Group, solo.Group);
        context.BoardFields.AddRange(legacy.Field, modern.Field, solo.Field);
        await context.SaveChangesAsync();

        return new Seeded(
            legacy.Board.Id, legacy.Group.Id, legacy.Field.Id,
            modern.Board.Id, modern.Group.Id, modern.Field.Id,
            solo.Board.Id, solo.Group.Id, solo.Field.Id);
    }

    private async Task<Guid[]> InsertOptionsAsync(Guid fieldId, params (string Name, string Position, string Category)[] options)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        var ids = new List<Guid>();
        foreach (var (name, position, category) in options)
        {
            var id = Guid.NewGuid();
            await using var command = new NpgsqlCommand(
                "INSERT INTO work.field_options (id, field_id, name, color, position, category) VALUES (@id, @field, @name, '#808080', @position, @category)",
                connection);
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("field", fieldId);
            command.Parameters.AddWithValue("name", name);
            command.Parameters.AddWithValue("position", position);
            command.Parameters.AddWithValue("category", category);
            await command.ExecuteNonQueryAsync();
            ids.Add(id);
        }

        return ids.ToArray();
    }

    /// <summary>
    /// Inserts an Item through the domain (so every required column is set), then
    /// its Status value and timestamps directly, as legacy rows would hold them.
    /// </summary>
    private async Task<Guid> InsertItemAsync(
        Guid boardId, Guid groupId, string title, Guid statusFieldId, Guid optionId,
        DateTimeOffset? completedAt = null, bool deleted = false)
    {
        Guid itemId;
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using (var context = CreateContext(tenant))
        {
            var board = await context.Boards.SingleAsync(b => b.Id == boardId);
            var item = BoardItem.CreateRoot(board.AccountId, board.WorkspaceId, boardId, groupId, title,
                FractionalIndex.Create($"a{++_positions}"), Guid.NewGuid(), Now);
            context.BoardItems.Add(item);
            await context.SaveChangesAsync();
            itemId = item.Id;
        }

        await ExecuteAsync($"""
            INSERT INTO work.board_item_values (id, item_id, field_id, value) VALUES ('{Guid.NewGuid()}', '{itemId}', '{statusFieldId}', '"{optionId}"'::jsonb);
            UPDATE work.board_items SET updated_at = '{Now.AddHours(2):O}', completed_at = {(completedAt is { } at ? $"'{at:O}'" : "NULL")},
                   deleted_at = {(deleted ? $"'{Now.AddHours(3):O}'" : "NULL")}
             WHERE id = '{itemId}';
            """);
        return itemId;
    }

    private Task<List<string>> CategoriesAsync(Guid fieldId) => QueryAsync(
        $"SELECT name || '=' || coalesce(category, '<null>') FROM work.field_options WHERE field_id = '{fieldId}' ORDER BY position COLLATE \"C\"");

    private async Task<Dictionary<Guid, DateTimeOffset?>> CompletionAsync()
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT id, completed_at FROM work.board_items", connection);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new Dictionary<Guid, DateTimeOffset?>();
        while (await reader.ReadAsync())
            rows[reader.GetGuid(0)] = reader.IsDBNull(1) ? null : reader.GetFieldValue<DateTimeOffset>(1);
        return rows;
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
