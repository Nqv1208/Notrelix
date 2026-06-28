using Microsoft.Data.Sqlite;
using Notrelix.Application.Common.Abstractions;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Core;
using Notrelix.Testing.Domain.Builders;

namespace Notrelix.Integration.Tests.Integration;

/// <summary>
/// Runtime cross-tenant isolation tests using SQLite InMemory.
///
/// SQLite enforces EF Core global query filters at query time (unlike the EF Core InMemory provider),
/// so these tests prove workspace isolation works at the database query level, not just at the
/// model configuration level.
///
/// EF Core replaces the context-instance reference in query filter expressions at query time,
/// so switching FakeCurrentWorkspace.WorkspaceId on the same instance produces correct filter behavior.
/// </summary>
public class CrossTenantIsolationTests : IDisposable
{
    private static readonly Guid OwnerId = Guid.Parse("00000000-0000-0000-0000-000000000099");
    private static readonly DateTimeOffset FixedTime = new(2026, 6, 28, 0, 0, 0, TimeSpan.Zero);

    private readonly List<SqliteConnection> _connections = new();

    public void Dispose()
    {
        foreach (var conn in _connections)
        {
            conn.Close();
            conn.Dispose();
        }
    }

    private (ApplicationDbContext context, SqliteConnection connection) CreateContext(ICurrentWorkspace workspace)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        _connections.Add(connection);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ApplicationDbContext(options, workspace);
        context.Database.EnsureCreated();
        return (context, connection);
    }

    // ============================================================
    // Part A — Runtime cross-tenant isolation
    // ============================================================

    [Fact]
    public async Task UserInWorkspaceA_CannotSeeWorkspaceB_Boards()
    {
        var wsA = TestIds.NewWorkspaceId();
        var wsB = TestIds.NewWorkspaceId();

        var workspace = new FakeCurrentWorkspace();
        workspace.EnterSystemContext();
        var (context, _) = CreateContext(workspace);

        var workspaceA = Workspace.Create(OwnerId, "Workspace A", "ws-a", FixedTime);
        var workspaceB = Workspace.Create(OwnerId, "Workspace B", "ws-b", FixedTime);
        context.Workspaces.AddRange(workspaceA, workspaceB);

        context.Boards.Add(new BoardBuilder()
            .WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A").WithCreatedAt(FixedTime).Build());
        context.Boards.Add(new BoardBuilder()
            .WithWorkspaceId(wsB).WithCreatedBy(OwnerId).WithTitle("Board B").WithCreatedAt(FixedTime).Build());

        await context.SaveChangesAsync();

        workspace.SetWorkspace(wsA);

        var boards = await context.Boards.ToListAsync();
        boards.Should().AllSatisfy(b => b.WorkspaceId.Should().Be(wsA));
        boards.Should().HaveCount(1);
    }

    [Fact]
    public async Task UserInWorkspaceB_CannotSeeWorkspaceA_Boards()
    {
        var wsA = TestIds.NewWorkspaceId();
        var wsB = TestIds.NewWorkspaceId();

        var workspace = new FakeCurrentWorkspace();
        workspace.EnterSystemContext();
        var (context, _) = CreateContext(workspace);

        var workspaceA = Workspace.Create(OwnerId, "Workspace A", "ws-a", FixedTime);
        var workspaceB = Workspace.Create(OwnerId, "Workspace B", "ws-b", FixedTime);
        context.Workspaces.AddRange(workspaceA, workspaceB);

        context.Boards.Add(new BoardBuilder()
            .WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A").WithCreatedAt(FixedTime).Build());
        context.Boards.Add(new BoardBuilder()
            .WithWorkspaceId(wsB).WithCreatedBy(OwnerId).WithTitle("Board B").WithCreatedAt(FixedTime).Build());

        await context.SaveChangesAsync();

        workspace.SetWorkspace(wsB);

        var boards = await context.Boards.ToListAsync();
        boards.Should().AllSatisfy(b => b.WorkspaceId.Should().Be(wsB));
        boards.Should().HaveCount(1);
    }

    [Fact]
    public async Task EachWorkspace_SeesOnlyOwnBoards_WhenBothWorkspacesHaveData()
    {
        var wsA = TestIds.NewWorkspaceId();
        var wsB = TestIds.NewWorkspaceId();

        var workspace = new FakeCurrentWorkspace();
        workspace.EnterSystemContext();
        var (context, _) = CreateContext(workspace);

        var workspaceA = Workspace.Create(OwnerId, "Workspace A", "ws-a", FixedTime);
        var workspaceB = Workspace.Create(OwnerId, "Workspace B", "ws-b", FixedTime);
        context.Workspaces.AddRange(workspaceA, workspaceB);

        context.Boards.AddRange(
            new BoardBuilder().WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A1").WithCreatedAt(FixedTime).Build(),
            new BoardBuilder().WithWorkspaceId(wsB).WithCreatedBy(OwnerId).WithTitle("Board B1").WithCreatedAt(FixedTime).Build(),
            new BoardBuilder().WithWorkspaceId(wsB).WithCreatedBy(OwnerId).WithTitle("Board B2").WithCreatedAt(FixedTime).Build());

        await context.SaveChangesAsync();

        workspace.SetWorkspace(wsA);
        var boardsA = await context.Boards.ToListAsync();
        boardsA.Should().HaveCount(1);
        boardsA.Should().AllSatisfy(b => b.WorkspaceId.Should().Be(wsA));

        workspace.SetWorkspace(wsB);
        var boardsB = await context.Boards.ToListAsync();
        boardsB.Should().HaveCount(2);
        boardsB.Should().AllSatisfy(b => b.WorkspaceId.Should().Be(wsB));
    }

    [Fact]
    public async Task CrossTenantBoardQuery_ReturnsEmpty_WhenWorkspaceDiffers()
    {
        var wsA = TestIds.NewWorkspaceId();
        var wsB = TestIds.NewWorkspaceId();

        var workspace = new FakeCurrentWorkspace();
        workspace.EnterSystemContext();
        var (context, _) = CreateContext(workspace);

        var boardA = new BoardBuilder()
            .WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A").WithCreatedAt(FixedTime).Build();
        context.Boards.Add(boardA);
        await context.SaveChangesAsync();

        workspace.SetWorkspace(wsA);

        var boardFromB = await context.Boards
            .FirstOrDefaultAsync(b => b.Id == boardA.Id);
        boardFromB.Should().NotBeNull("board A should be visible in workspace A");

        workspace.SetWorkspace(wsB);

        var boardFromOtherWorkspace = await context.Boards
            .FirstOrDefaultAsync(b => b.Id == boardA.Id);
        boardFromOtherWorkspace.Should().BeNull("workspace filter should hide board A from workspace B");
    }

    // ============================================================
    // Part B — EF query filter runtime behavior
    // ============================================================

    [Fact]
    public async Task WhenNoWorkspaceSet_ScopedDbSets_ReturnNoRecords()
    {
        var wsA = TestIds.NewWorkspaceId();

        var workspace = new FakeCurrentWorkspace();
        workspace.EnterSystemContext();
        var (context, _) = CreateContext(workspace);

        context.Boards.Add(new BoardBuilder()
            .WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board").WithCreatedAt(FixedTime).Build());
        await context.SaveChangesAsync();

        workspace.Clear();

        var boards = await context.Boards.ToListAsync();
        boards.Should().BeEmpty("no workspace context should block all workspace-scoped access");
    }

    [Fact]
    public async Task WhenWorkspaceASet_WorkspaceBRecords_AreFilteredOut()
    {
        var wsA = TestIds.NewWorkspaceId();
        var wsB = TestIds.NewWorkspaceId();

        var workspace = new FakeCurrentWorkspace();
        workspace.EnterSystemContext();
        var (context, _) = CreateContext(workspace);

        context.Boards.AddRange(
            new BoardBuilder().WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A").WithCreatedAt(FixedTime).Build(),
            new BoardBuilder().WithWorkspaceId(wsB).WithCreatedBy(OwnerId).WithTitle("Board B").WithCreatedAt(FixedTime).Build());

        await context.SaveChangesAsync();

        workspace.SetWorkspace(wsA);

        var boardsInA = await context.Boards.ToListAsync();
        boardsInA.Should().HaveCount(1);
        boardsInA[0].Title.Should().Be("Board A");

        var boardsExplicitlyInB = await context.Boards.Where(b => b.WorkspaceId == wsB).ToListAsync();
        boardsExplicitlyInB.Should().BeEmpty("workspace filter + explicit Where should still produce no results for other workspace");
    }

    [Fact]
    public async Task SystemContext_BypassesWorkspaceFilter()
    {
        var wsA = TestIds.NewWorkspaceId();
        var wsB = TestIds.NewWorkspaceId();

        var workspace = new FakeCurrentWorkspace();
        workspace.EnterSystemContext();
        var (context, _) = CreateContext(workspace);

        context.Boards.AddRange(
            new BoardBuilder().WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A").WithCreatedAt(FixedTime).Build(),
            new BoardBuilder().WithWorkspaceId(wsB).WithCreatedBy(OwnerId).WithTitle("Board B").WithCreatedAt(FixedTime).Build());

        await context.SaveChangesAsync();

        var boards = await context.Boards.ToListAsync();
        boards.Should().HaveCount(2, "system context should see all workspaces");
    }

    [Fact]
    public async Task SystemContext_StillAppliesSoftDeleteFilter()
    {
        var workspace = new FakeCurrentWorkspace();
        workspace.EnterSystemContext();
        var (context, _) = CreateContext(workspace);

        var board = new BoardBuilder()
            .WithWorkspaceId(TestIds.NewWorkspaceId()).WithCreatedBy(OwnerId).WithTitle("To Delete").WithCreatedAt(FixedTime).Build();
        context.Boards.Add(board);
        await context.SaveChangesAsync();

        board.SoftDelete(OwnerId, FixedTime);
        await context.SaveChangesAsync();

        var activeBoards = await context.Boards.ToListAsync();
        activeBoards.Should().BeEmpty("soft-deleted entity should be hidden even in system context");
    }

    [Fact]
    public async Task SoftDeleted_Boards_InOtherWorkspace_AreInvisible()
    {
        var wsA = TestIds.NewWorkspaceId();
        var wsB = TestIds.NewWorkspaceId();

        var workspace = new FakeCurrentWorkspace();
        workspace.EnterSystemContext();
        var (context, _) = CreateContext(workspace);

        var boardA = new BoardBuilder()
            .WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A Active").WithCreatedAt(FixedTime).Build();
        var boardADeleted = new BoardBuilder()
            .WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A Deleted").WithCreatedAt(FixedTime).Build();
        var boardB = new BoardBuilder()
            .WithWorkspaceId(wsB).WithCreatedBy(OwnerId).WithTitle("Board B Active").WithCreatedAt(FixedTime).Build();

        context.Boards.AddRange(boardA, boardADeleted, boardB);
        await context.SaveChangesAsync();

        boardADeleted.SoftDelete(OwnerId, FixedTime);
        await context.SaveChangesAsync();

        workspace.SetWorkspace(wsA);
        var boards = await context.Boards.ToListAsync();
        boards.Should().HaveCount(1, "only active board in workspace A should be visible");
        boards.Should().Contain(b => b.Title == "Board A Active");
        boards.Should().NotContain(b => b.Title == "Board A Deleted");
        boards.Should().NotContain(b => b.Title == "Board B Active");
    }

    [Fact]
    public async Task SwitchingWorkspace_ChangesQueryResults()
    {
        var wsA = TestIds.NewWorkspaceId();
        var wsB = TestIds.NewWorkspaceId();

        var workspace = new FakeCurrentWorkspace();
        workspace.EnterSystemContext();
        var (context, _) = CreateContext(workspace);

        var workspaceA = Workspace.Create(OwnerId, "WS A", "ws-a", FixedTime);
        var workspaceB = Workspace.Create(OwnerId, "WS B", "ws-b", FixedTime);
        context.Workspaces.AddRange(workspaceA, workspaceB);

        var boardA = new BoardBuilder()
            .WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A").WithCreatedAt(FixedTime).Build();
        var boardB = new BoardBuilder()
            .WithWorkspaceId(wsB).WithCreatedBy(OwnerId).WithTitle("Board B").WithCreatedAt(FixedTime).Build();
        context.Boards.AddRange(boardA, boardB);
        await context.SaveChangesAsync();

        workspace.SetWorkspace(wsA);
        var countA = await context.Boards.CountAsync();

        workspace.SetWorkspace(wsB);
        var countB = await context.Boards.CountAsync();

        countA.Should().Be(1, "workspace A should see 1 board");
        countB.Should().Be(1, "workspace B should see 1 board");
    }

    [Fact]
    public async Task NonWorkspaceScopedEntities_AreNotAffectedByWorkspaceFilter()
    {
        var workspace = new FakeCurrentWorkspace();
        workspace.EnterSystemContext();
        var (context, _) = CreateContext(workspace);

        var wsA = Workspace.Create(OwnerId, "WS A", "ws-a", FixedTime);
        context.Workspaces.Add(wsA);
        await context.SaveChangesAsync();

        workspace.SetWorkspace(TestIds.NewWorkspaceId());

        var workspaces = await context.Workspaces.ToListAsync();
        workspaces.Should().HaveCount(1, "Workspace does not implement IWorkspaceScoped and should not be filtered by workspace");
    }

    [Fact]
    public async Task IgnoreQueryFilters_CanRetrieveFilteredData()
    {
        var wsA = TestIds.NewWorkspaceId();
        var wsB = TestIds.NewWorkspaceId();

        var workspace = new FakeCurrentWorkspace();
        workspace.EnterSystemContext();
        var (context, _) = CreateContext(workspace);

        context.Boards.AddRange(
            new BoardBuilder().WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A").WithCreatedAt(FixedTime).Build(),
            new BoardBuilder().WithWorkspaceId(wsB).WithCreatedBy(OwnerId).WithTitle("Board B").WithCreatedAt(FixedTime).Build());

        await context.SaveChangesAsync();

        workspace.SetWorkspace(wsA);

        var allBoards = await context.Boards.IgnoreQueryFilters().ToListAsync();
        allBoards.Should().HaveCount(2, "IgnoreQueryFilters should bypass workspace filter");
    }
}
