using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Core;
using Notrelix.Testing.Domain.Builders;

namespace Notrelix.Integration.Tests.Integration;

/// <summary>
/// Runtime cross-tenant isolation tests using PostgreSQL.
///
/// These tests verify that EF Core global query filters enforce workspace isolation
/// at the database query level, not just at the model configuration level.
/// Running on PostgreSQL ensures test behavior matches production.
///
/// Database is reset (TRUNCATE CASCADE) before each test to prevent cross-test contamination.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public class CrossTenantIsolationTests : IAsyncLifetime
{
    private static readonly Guid AccountId = Guid.Parse("00000000-0000-0000-0000-000000000088");
    private static readonly Guid OwnerId = Guid.Parse("00000000-0000-0000-0000-000000000099");
    private static readonly DateTimeOffset FixedTime = new(2026, 6, 28, 0, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _fixture;
    private DatabaseReset _reset = null!;

    public CrossTenantIsolationTests(PostgresTestContainer fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_fixture.ConnectionString);
        await _reset.ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private ApplicationDbContext CreateContext(ICurrentTenantContext tenant)
    {
        return _fixture.CreateContext(tenant);
    }

    // ============================================================
    // Part A — Runtime cross-tenant isolation
    // ============================================================

    [Fact]
    public async Task UserInWorkspaceA_CannotSeeWorkspaceB_Boards()
    {
        var wsA = TestIds.NewWorkspaceId();
        var wsB = TestIds.NewWorkspaceId();

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        var workspaceA = Workspace.Create(Guid.NewGuid(), OwnerId, "Workspace A", "ws-a", FixedTime);
        var workspaceB = Workspace.Create(Guid.NewGuid(), OwnerId, "Workspace B", "ws-b", FixedTime);
        context.Workspaces.AddRange(workspaceA, workspaceB);

        context.Boards.Add(new BoardBuilder()
            .WithAccountId(AccountId).WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A").WithCreatedAt(FixedTime).Build());
        context.Boards.Add(new BoardBuilder()
            .WithAccountId(AccountId).WithWorkspaceId(wsB).WithCreatedBy(OwnerId).WithTitle("Board B").WithCreatedAt(FixedTime).Build());

        await context.SaveChangesAsync();

        tenant.SetWorkspace(AccountId, wsA, null);

        var boards = await context.Boards.ToListAsync();
        boards.Should().AllSatisfy(b => b.WorkspaceId.Should().Be(wsA));
        boards.Should().HaveCount(1);
    }

    [Fact]
    public async Task UserInWorkspaceB_CannotSeeWorkspaceA_Boards()
    {
        var wsA = TestIds.NewWorkspaceId();
        var wsB = TestIds.NewWorkspaceId();

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        var workspaceA = Workspace.Create(Guid.NewGuid(), OwnerId, "Workspace A", "ws-a", FixedTime);
        var workspaceB = Workspace.Create(Guid.NewGuid(), OwnerId, "Workspace B", "ws-b", FixedTime);
        context.Workspaces.AddRange(workspaceA, workspaceB);

        context.Boards.Add(new BoardBuilder()
            .WithAccountId(AccountId).WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A").WithCreatedAt(FixedTime).Build());
        context.Boards.Add(new BoardBuilder()
            .WithAccountId(AccountId).WithWorkspaceId(wsB).WithCreatedBy(OwnerId).WithTitle("Board B").WithCreatedAt(FixedTime).Build());

        await context.SaveChangesAsync();

        tenant.SetWorkspace(AccountId, wsB, null);

        var boards = await context.Boards.ToListAsync();
        boards.Should().AllSatisfy(b => b.WorkspaceId.Should().Be(wsB));
        boards.Should().HaveCount(1);
    }

    [Fact]
    public async Task EachWorkspace_SeesOnlyOwnBoards_WhenBothWorkspacesHaveData()
    {
        var wsA = TestIds.NewWorkspaceId();
        var wsB = TestIds.NewWorkspaceId();

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        var workspaceA = Workspace.Create(Guid.NewGuid(), OwnerId, "Workspace A", "ws-a", FixedTime);
        var workspaceB = Workspace.Create(Guid.NewGuid(), OwnerId, "Workspace B", "ws-b", FixedTime);
        context.Workspaces.AddRange(workspaceA, workspaceB);

        context.Boards.AddRange(
            new BoardBuilder().WithAccountId(AccountId).WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A1").WithCreatedAt(FixedTime).Build(),
            new BoardBuilder().WithAccountId(AccountId).WithWorkspaceId(wsB).WithCreatedBy(OwnerId).WithTitle("Board B1").WithCreatedAt(FixedTime).Build(),
            new BoardBuilder().WithAccountId(AccountId).WithWorkspaceId(wsB).WithCreatedBy(OwnerId).WithTitle("Board B2").WithCreatedAt(FixedTime).Build());

        await context.SaveChangesAsync();

        tenant.SetWorkspace(AccountId, wsA, null);
        var boardsA = await context.Boards.ToListAsync();
        boardsA.Should().HaveCount(1);
        boardsA.Should().AllSatisfy(b => b.WorkspaceId.Should().Be(wsA));

        tenant.SetWorkspace(AccountId, wsB, null);
        var boardsB = await context.Boards.ToListAsync();
        boardsB.Should().HaveCount(2);
        boardsB.Should().AllSatisfy(b => b.WorkspaceId.Should().Be(wsB));
    }

    [Fact]
    public async Task CrossTenantBoardQuery_ReturnsEmpty_WhenWorkspaceDiffers()
    {
        var wsA = TestIds.NewWorkspaceId();
        var wsB = TestIds.NewWorkspaceId();

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        var boardA = new BoardBuilder()
            .WithAccountId(AccountId).WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A").WithCreatedAt(FixedTime).Build();
        context.Boards.Add(boardA);
        await context.SaveChangesAsync();

        tenant.SetWorkspace(AccountId, wsA, null);

        var boardFromB = await context.Boards
            .FirstOrDefaultAsync(b => b.Id == boardA.Id);
        boardFromB.Should().NotBeNull("board A should be visible in workspace A");

        tenant.SetWorkspace(AccountId, wsB, null);

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

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        context.Boards.Add(new BoardBuilder()
            .WithAccountId(AccountId).WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board").WithCreatedAt(FixedTime).Build());
        await context.SaveChangesAsync();

        tenant.Clear();

        var boards = await context.Boards.ToListAsync();
        boards.Should().BeEmpty("no workspace context should block all workspace-scoped access");
    }

    [Fact]
    public async Task WhenWorkspaceASet_WorkspaceBRecords_AreFilteredOut()
    {
        var wsA = TestIds.NewWorkspaceId();
        var wsB = TestIds.NewWorkspaceId();

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        context.Boards.AddRange(
            new BoardBuilder().WithAccountId(AccountId).WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A").WithCreatedAt(FixedTime).Build(),
            new BoardBuilder().WithAccountId(AccountId).WithWorkspaceId(wsB).WithCreatedBy(OwnerId).WithTitle("Board B").WithCreatedAt(FixedTime).Build());

        await context.SaveChangesAsync();

        tenant.SetWorkspace(AccountId, wsA, null);

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

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        context.Boards.AddRange(
            new BoardBuilder().WithAccountId(AccountId).WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A").WithCreatedAt(FixedTime).Build(),
            new BoardBuilder().WithAccountId(AccountId).WithWorkspaceId(wsB).WithCreatedBy(OwnerId).WithTitle("Board B").WithCreatedAt(FixedTime).Build());

        await context.SaveChangesAsync();

        var boards = await context.Boards.ToListAsync();
        boards.Should().HaveCount(2, "system context should see all workspaces");
    }

    [Fact]
    public async Task SystemContext_StillAppliesSoftDeleteFilter()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        var board = new BoardBuilder()
            .WithAccountId(AccountId).WithWorkspaceId(TestIds.NewWorkspaceId()).WithCreatedBy(OwnerId).WithTitle("To Delete").WithCreatedAt(FixedTime).Build();
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

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        var boardA = new BoardBuilder()
            .WithAccountId(AccountId).WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A Active").WithCreatedAt(FixedTime).Build();
        var boardADeleted = new BoardBuilder()
            .WithAccountId(AccountId).WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A Deleted").WithCreatedAt(FixedTime).Build();
        var boardB = new BoardBuilder()
            .WithAccountId(AccountId).WithWorkspaceId(wsB).WithCreatedBy(OwnerId).WithTitle("Board B Active").WithCreatedAt(FixedTime).Build();

        context.Boards.AddRange(boardA, boardADeleted, boardB);
        await context.SaveChangesAsync();

        boardADeleted.SoftDelete(OwnerId, FixedTime);
        await context.SaveChangesAsync();

        tenant.SetWorkspace(AccountId, wsA, null);
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

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        var workspaceA = Workspace.Create(Guid.NewGuid(), OwnerId, "WS A", "ws-a", FixedTime);
        var workspaceB = Workspace.Create(Guid.NewGuid(), OwnerId, "WS B", "ws-b", FixedTime);
        context.Workspaces.AddRange(workspaceA, workspaceB);

        var boardA = new BoardBuilder()
            .WithAccountId(AccountId).WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A").WithCreatedAt(FixedTime).Build();
        var boardB = new BoardBuilder()
            .WithAccountId(AccountId).WithWorkspaceId(wsB).WithCreatedBy(OwnerId).WithTitle("Board B").WithCreatedAt(FixedTime).Build();
        context.Boards.AddRange(boardA, boardB);
        await context.SaveChangesAsync();

        tenant.SetWorkspace(AccountId, wsA, null);
        var countA = await context.Boards.CountAsync();

        tenant.SetWorkspace(AccountId, wsB, null);
        var countB = await context.Boards.CountAsync();

        countA.Should().Be(1, "workspace A should see 1 board");
        countB.Should().Be(1, "workspace B should see 1 board");
    }

    [Fact]
    public async Task NonWorkspaceScopedEntities_AreNotAffectedByWorkspaceFilter()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        var wsA = Workspace.Create(Guid.NewGuid(), OwnerId, "WS A", "ws-a", FixedTime);
        context.Workspaces.Add(wsA);
        await context.SaveChangesAsync();

        tenant.SetWorkspace(AccountId, TestIds.NewWorkspaceId(), null);

        var workspaces = await context.Workspaces.ToListAsync();
        workspaces.Should().HaveCount(1, "Workspace does not implement IWorkspaceScoped and should not be filtered by workspace");
    }

    [Fact]
    public async Task IgnoreQueryFilters_CanRetrieveFilteredData()
    {
        var wsA = TestIds.NewWorkspaceId();
        var wsB = TestIds.NewWorkspaceId();

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        context.Boards.AddRange(
            new BoardBuilder().WithAccountId(AccountId).WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A").WithCreatedAt(FixedTime).Build(),
            new BoardBuilder().WithAccountId(AccountId).WithWorkspaceId(wsB).WithCreatedBy(OwnerId).WithTitle("Board B").WithCreatedAt(FixedTime).Build());

        await context.SaveChangesAsync();

        tenant.SetWorkspace(AccountId, wsA, null);

        var allBoards = await context.Boards.IgnoreQueryFilters().ToListAsync();
        allBoards.Should().HaveCount(2, "IgnoreQueryFilters should bypass workspace filter");
    }
}
