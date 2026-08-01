using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.WorkManagement;

[Collection("Database")]
public class BoardFieldDefaultValuePersistenceTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public BoardFieldDefaultValuePersistenceTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task DefaultValue_RoundTrips_AsFieldValue()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var ctx = _db.CreateContext(tenant);

        var boardId = await SeedBoardAsync(ctx);

        var defaultValue = FieldValue.Create(JsonValue.Create("\"hello\""));
        var field = BoardField.Create(
            Guid.NewGuid(), Guid.NewGuid(), boardId,
            "Notes", FieldType.Text, FieldSettings.Empty(),
            FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);
        field.SetDefaultValue(defaultValue, Guid.NewGuid(), DateTimeOffset.UtcNow);

        ctx.BoardFields.Add(field);
        await ctx.SaveChangesAsync();

        await using var fresh = _db.CreateContext(tenant);
        var loaded = await fresh.BoardFields
            .IgnoreQueryFilters()
            .FirstAsync(f => f.Id == field.Id);

        loaded.DefaultValue.Should().NotBeNull();
        loaded.DefaultValue!.Data.Value.Should().Be(defaultValue.Data.Value);
    }

    [Fact]
    public async Task NullDefault_RoundTrips_AsNull()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var ctx = _db.CreateContext(tenant);

        var boardId = await SeedBoardAsync(ctx);

        var field = BoardField.Create(
            Guid.NewGuid(), Guid.NewGuid(), boardId,
            "Notes", FieldType.Text, FieldSettings.Empty(),
            FractionalIndex.Create("a0"), Guid.NewGuid(), DateTimeOffset.UtcNow);

        ctx.BoardFields.Add(field);
        await ctx.SaveChangesAsync();

        await using var fresh = _db.CreateContext(tenant);
        var loaded = await fresh.BoardFields
            .IgnoreQueryFilters()
            .FirstAsync(f => f.Id == field.Id);

        loaded.DefaultValue.Should().BeNull();
    }

    private static async Task<Guid> SeedBoardAsync(ApplicationDbContext context)
    {
        var now = DateTimeOffset.UtcNow;
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create(Guid.NewGuid(), ownerId, "Workspace", "workspace", now);
        var board = Board.Create(Guid.NewGuid(), workspace.Id, ownerId, "Board", null, now);

        context.Workspaces.Add(workspace);
        context.Boards.Add(board);
        await context.SaveChangesAsync();

        return board.Id;
    }
}
