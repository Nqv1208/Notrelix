using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Xunit.Abstractions;

namespace Notrelix.Integration.Tests.WorkManagement;

[Collection("Database")]
public class BoardVisibilityDirectTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly ITestOutputHelper _output;
    private DatabaseReset _reset = null!;

    public BoardVisibilityDirectTests(PostgresTestContainer db, ITestOutputHelper output)
    {
        _db = db;
        _output = output;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SaveAndReadPrivateVisibility()
    {
        var workspace = new FakeCurrentWorkspace();
        workspace.EnterSystemContext();
        await using var ctx = _db.CreateContext(workspace);

        var board = Board.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test", null, DateTimeOffset.UtcNow,
            BoardVisibility.Private);

        ctx.Boards.Add(board);
        await ctx.SaveChangesAsync();

        var entityType = ctx.Model.FindEntityType(typeof(Board));
        var visibilityProp = entityType!.FindProperty(nameof(Board.Visibility));
        var converter = visibilityProp!.GetValueConverter();
        var columnType = visibilityProp.GetColumnType();
        _output.WriteLine($"Column type: {columnType}");
        _output.WriteLine($"Converter type: {converter?.GetType().Name}");
        _output.WriteLine($"Converter converts nulls: {converter?.ConvertsNulls}");

        // Raw SQL check
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT visibility FROM work.boards WHERE id = @id";
        cmd.Parameters.Add(new Npgsql.NpgsqlParameter("id", board.Id));
        var raw = await cmd.ExecuteScalarAsync();
        _output.WriteLine($"Raw DB value: '{raw}' (type: {raw?.GetType()})");

        // Check if EF Core even issues a query vs using tracked entity
        var saved = await ctx.Boards
            .IgnoreQueryFilters()
            .FirstAsync(b => b.Id == board.Id);

        _output.WriteLine($"EF Core read: {saved.Visibility} (int: {(int)saved.Visibility})");
        Assert.Equal(BoardVisibility.Private, saved.Visibility);
    }

    [Fact]
    public async Task SaveAndReadPrivate_FreshContext()
    {
        var workspace = new FakeCurrentWorkspace();
        workspace.EnterSystemContext();

        Guid boardId;
        {
            await using var ctx1 = _db.CreateContext(workspace);
            var board = Board.Create(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                "Test", null, DateTimeOffset.UtcNow,
                BoardVisibility.Private);
            boardId = board.Id;
            ctx1.Boards.Add(board);
            await ctx1.SaveChangesAsync();
        }

        await using var ctx2 = _db.CreateContext(workspace);

        var entityType = ctx2.Model.FindEntityType(typeof(Board));
        var visibilityProp = entityType!.FindProperty(nameof(Board.Visibility));
        var converter = visibilityProp!.GetValueConverter();
        _output.WriteLine($"Fresh ctx - Converter type: {converter?.GetType().Name}");

        // Raw SQL check
        await using var conn = _db.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT visibility FROM work.boards WHERE id = @id";
        cmd.Parameters.Add(new Npgsql.NpgsqlParameter("id", boardId));
        var raw = await cmd.ExecuteScalarAsync();
        _output.WriteLine($"Fresh ctx - Raw DB value: '{raw}'");

        var saved = await ctx2.Boards
            .IgnoreQueryFilters()
            .FirstAsync(b => b.Id == boardId);

        _output.WriteLine($"Fresh ctx - EF Core read: {saved.Visibility} (int: {(int)saved.Visibility})");
        Assert.Equal(BoardVisibility.Private, saved.Visibility);
    }
}
