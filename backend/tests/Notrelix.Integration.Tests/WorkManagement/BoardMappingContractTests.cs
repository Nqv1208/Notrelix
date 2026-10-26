using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// MAP-VALID-001 / BOARD-MAP-001 (WM-V3-DATA-004, WMREQ094–096). Persistence must
/// accept every Domain-valid Board boundary value on real PostgreSQL.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class BoardMappingContractTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;

    public BoardMappingContractTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.OwnerConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Board_WithDomainMaximumTitleAndDescription_PersistsAndRoundTrips()
    {
        var now = DateTimeOffset.UtcNow;
        var accountId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var workspace = Workspace.Create(accountId, ownerId, "Mapping WS", $"map-{Guid.NewGuid():N}", now);
        var title = new string('t', 255);
        var description = new string('d', 5000);
        var board = Board.Create(accountId, workspace.Id, ownerId, title, description, now);

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using (var context = _db.CreateOwnerContext(tenant))
        {
            context.Workspaces.Add(workspace);
            context.Boards.Add(board);
            await context.SaveChangesAsync();
        }

        await using var verify = _db.CreateOwnerContext(tenant);
        var loaded = await verify.Boards.SingleAsync(b => b.Id == board.Id);
        loaded.Title.Should().Be(title);
        loaded.Description.Should().Be(description);
    }
}
