using Notrelix.Domain.Collaboration.Comments;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Forms;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Infrastructure.Data;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Testing.Core;
using Notrelix.Testing.Domain.Builders;

using Notrelix.Domain.SharedKernel.Ordering;
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
    public async Task SystemContext_StillAppliesDeleteFilter()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        var board = new BoardBuilder()
            .WithAccountId(AccountId).WithWorkspaceId(TestIds.NewWorkspaceId()).WithCreatedBy(OwnerId).WithTitle("To Delete").WithCreatedAt(FixedTime).Build();
        context.Boards.Add(board);
        await context.SaveChangesAsync();

        board.Delete(OwnerId, FixedTime);
        await context.SaveChangesAsync();

        var activeBoards = await context.Boards.ToListAsync();
        activeBoards.Should().BeEmpty("soft-deleted entity should be hidden even in system context");
    }

    [Fact]
    public async Task Deleted_Boards_InOtherWorkspace_AreInvisible()
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

        boardADeleted.Delete(OwnerId, FixedTime);
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

    // ============================================================
    // Part C — Extended cross-tenant isolation (7 entity types)
    // Each test creates boards first, then child entities per workspace.
    // ============================================================

    [Fact]
    public async Task BoardItem_CrossTenant_IsIsolated()
    {
        var wsA = TestIds.NewWorkspaceId();
        var wsB = TestIds.NewWorkspaceId();

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        var boardA = new BoardBuilder().WithAccountId(AccountId).WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A").WithCreatedAt(FixedTime).Build();
        var boardB = new BoardBuilder().WithAccountId(AccountId).WithWorkspaceId(wsB).WithCreatedBy(OwnerId).WithTitle("Board B").WithCreatedAt(FixedTime).Build();
        context.Boards.AddRange(boardA, boardB);
        await context.SaveChangesAsync();

        var groupA = BoardGroup.Create(AccountId, wsA, boardA.Id, "Group A", Color.Create("#0079BF"), FractionalIndex.Initial(), OwnerId, FixedTime);
        var groupB = BoardGroup.Create(AccountId, wsB, boardB.Id, "Group B", Color.Create("#0079BF"), FractionalIndex.Initial(), OwnerId, FixedTime);
        context.BoardGroups.AddRange(groupA, groupB);
        await context.SaveChangesAsync();

        var itemA = BoardItem.CreateRoot(AccountId, wsA, boardA.Id, groupA.Id, "Item A", FractionalIndex.Initial(), OwnerId, FixedTime);
        var itemB = BoardItem.CreateRoot(AccountId, wsB, boardB.Id, groupB.Id, "Item B", FractionalIndex.Initial(), OwnerId, FixedTime);
        context.BoardItems.AddRange(itemA, itemB);
        await context.SaveChangesAsync();

        tenant.SetWorkspace(AccountId, wsA, null);
        var itemsInA = await context.BoardItems.ToListAsync();
        itemsInA.Should().HaveCount(1);
        itemsInA.Should().AllSatisfy(i => i.WorkspaceId.Should().Be(wsA));

        tenant.SetWorkspace(AccountId, wsB, null);
        var itemsInB = await context.BoardItems.ToListAsync();
        itemsInB.Should().HaveCount(1);
        itemsInB.Should().AllSatisfy(i => i.WorkspaceId.Should().Be(wsB));
    }

    [Fact]
    public async Task BoardField_CrossTenant_IsIsolated()
    {
        var wsA = TestIds.NewWorkspaceId();
        var wsB = TestIds.NewWorkspaceId();

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        var boardA = new BoardBuilder().WithAccountId(AccountId).WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A").WithCreatedAt(FixedTime).Build();
        var boardB = new BoardBuilder().WithAccountId(AccountId).WithWorkspaceId(wsB).WithCreatedBy(OwnerId).WithTitle("Board B").WithCreatedAt(FixedTime).Build();
        context.Boards.AddRange(boardA, boardB);
        await context.SaveChangesAsync();

        var fieldA = BoardField.Create(AccountId, wsA, boardA.Id, "Field A", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Initial(), OwnerId, FixedTime);
        var fieldB = BoardField.Create(AccountId, wsB, boardB.Id, "Field B", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Initial(), OwnerId, FixedTime);
        context.BoardFields.AddRange(fieldA, fieldB);
        await context.SaveChangesAsync();

        tenant.SetWorkspace(AccountId, wsA, null);
        var fieldsInA = await context.BoardFields.ToListAsync();
        fieldsInA.Should().HaveCount(1);
        fieldsInA.Should().AllSatisfy(f => f.WorkspaceId.Should().Be(wsA));

        tenant.SetWorkspace(AccountId, wsB, null);
        var fieldsInB = await context.BoardFields.ToListAsync();
        fieldsInB.Should().HaveCount(1);
        fieldsInB.Should().AllSatisfy(f => f.WorkspaceId.Should().Be(wsB));
    }

    [Fact]
    public async Task Page_CrossTenant_IsIsolated()
    {
        var wsA = TestIds.NewWorkspaceId();
        var wsB = TestIds.NewWorkspaceId();

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        var pageA = Page.Create(AccountId, wsA, "Page A", OwnerId, FixedTime);
        var pageB = Page.Create(AccountId, wsB, "Page B", OwnerId, FixedTime);
        context.Pages.AddRange(pageA, pageB);
        await context.SaveChangesAsync();

        tenant.SetWorkspace(AccountId, wsA, null);
        var pagesInA = await context.Pages.ToListAsync();
        pagesInA.Should().HaveCount(1);
        pagesInA.Should().AllSatisfy(p => p.WorkspaceId.Should().Be(wsA));

        tenant.SetWorkspace(AccountId, wsB, null);
        var pagesInB = await context.Pages.ToListAsync();
        pagesInB.Should().HaveCount(1);
        pagesInB.Should().AllSatisfy(p => p.WorkspaceId.Should().Be(wsB));
    }

    [Fact]
    public async Task Comment_CrossTenant_IsIsolated()
    {
        var wsA = TestIds.NewWorkspaceId();
        var wsB = TestIds.NewWorkspaceId();

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        var boardA = new BoardBuilder().WithAccountId(AccountId).WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A").WithCreatedAt(FixedTime).Build();
        var boardB = new BoardBuilder().WithAccountId(AccountId).WithWorkspaceId(wsB).WithCreatedBy(OwnerId).WithTitle("Board B").WithCreatedAt(FixedTime).Build();
        context.Boards.AddRange(boardA, boardB);
        await context.SaveChangesAsync();

        var targetA = ResourceRef.Create(ResourceKind.Create("work-management.board"), boardA.Id, wsA);
        var targetB = ResourceRef.Create(ResourceKind.Create("work-management.board"), boardB.Id, wsB);
        var commentA = Comment.Create(AccountId, wsA, targetA, "\"Comment A\"", OwnerId, FixedTime);
        var commentB = Comment.Create(AccountId, wsB, targetB, "\"Comment B\"", OwnerId, FixedTime);
        context.Comments.AddRange(commentA, commentB);
        await context.SaveChangesAsync();

        tenant.SetWorkspace(AccountId, wsA, null);
        var commentsInA = await context.Comments.ToListAsync();
        commentsInA.Should().HaveCount(1);
        commentsInA.Should().AllSatisfy(c => c.WorkspaceId.Should().Be(wsA));

        tenant.SetWorkspace(AccountId, wsB, null);
        var commentsInB = await context.Comments.ToListAsync();
        commentsInB.Should().HaveCount(1);
        commentsInB.Should().AllSatisfy(c => c.WorkspaceId.Should().Be(wsB));
    }

    [Fact]
    public async Task Form_CrossTenant_IsIsolated()
    {
        var wsA = TestIds.NewWorkspaceId();
        var wsB = TestIds.NewWorkspaceId();

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        var boardA = new BoardBuilder().WithAccountId(AccountId).WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A").WithCreatedAt(FixedTime).Build();
        var boardB = new BoardBuilder().WithAccountId(AccountId).WithWorkspaceId(wsB).WithCreatedBy(OwnerId).WithTitle("Board B").WithCreatedAt(FixedTime).Build();
        context.Boards.AddRange(boardA, boardB);
        await context.SaveChangesAsync();

        var formA = Form.Create(AccountId, wsA, boardA.Id, "Form A", "form-a", OwnerId, FixedTime);
        var formB = Form.Create(AccountId, wsB, boardB.Id, "Form B", "form-b", OwnerId, FixedTime);
        context.Forms.AddRange(formA, formB);
        await context.SaveChangesAsync();

        tenant.SetWorkspace(AccountId, wsA, null);
        var formsInA = await context.Forms.ToListAsync();
        formsInA.Should().HaveCount(1);
        formsInA.Should().AllSatisfy(f => f.WorkspaceId.Should().Be(wsA));

        tenant.SetWorkspace(AccountId, wsB, null);
        var formsInB = await context.Forms.ToListAsync();
        formsInB.Should().HaveCount(1);
        formsInB.Should().AllSatisfy(f => f.WorkspaceId.Should().Be(wsB));
    }

    [Fact]
    public async Task Space_CrossTenant_IsIsolated()
    {
        var wsA = TestIds.NewWorkspaceId();
        var wsB = TestIds.NewWorkspaceId();

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        var spaceA = Space.Create(AccountId, wsA, "Space A", SpaceVisibility.Private, OwnerId, FixedTime);
        var spaceB = Space.Create(AccountId, wsB, "Space B", SpaceVisibility.Private, OwnerId, FixedTime);
        context.Spaces.AddRange(spaceA, spaceB);
        await context.SaveChangesAsync();

        tenant.SetWorkspace(AccountId, wsA, null);
        var spacesInA = await context.Spaces.ToListAsync();
        spacesInA.Should().HaveCount(1);
        spacesInA.Should().AllSatisfy(s => s.WorkspaceId.Should().Be(wsA));

        tenant.SetWorkspace(AccountId, wsB, null);
        var spacesInB = await context.Spaces.ToListAsync();
        spacesInB.Should().HaveCount(1);
        spacesInB.Should().AllSatisfy(s => s.WorkspaceId.Should().Be(wsB));
    }

    [Fact]
    public async Task AllScopedEntities_CrossTenant_SimultaneouslyIsolated()
    {
        var wsA = TestIds.NewWorkspaceId();
        var wsB = TestIds.NewWorkspaceId();

        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        await using var context = CreateContext(tenant);

        var boardA = new BoardBuilder().WithAccountId(AccountId).WithWorkspaceId(wsA).WithCreatedBy(OwnerId).WithTitle("Board A").WithCreatedAt(FixedTime).Build();
        var boardB = new BoardBuilder().WithAccountId(AccountId).WithWorkspaceId(wsB).WithCreatedBy(OwnerId).WithTitle("Board B").WithCreatedAt(FixedTime).Build();
        context.Boards.AddRange(boardA, boardB);
        await context.SaveChangesAsync();

        var groupA = BoardGroup.Create(AccountId, wsA, boardA.Id, "Group A", Color.Create("#0079BF"), FractionalIndex.Initial(), OwnerId, FixedTime);
        var groupB = BoardGroup.Create(AccountId, wsB, boardB.Id, "Group B", Color.Create("#0079BF"), FractionalIndex.Initial(), OwnerId, FixedTime);
        context.BoardGroups.AddRange(groupA, groupB);
        await context.SaveChangesAsync();

        context.BoardItems.Add(BoardItem.CreateRoot(AccountId, wsA, boardA.Id, groupA.Id, "Item A", FractionalIndex.Initial(), OwnerId, FixedTime));
        context.BoardItems.Add(BoardItem.CreateRoot(AccountId, wsB, boardB.Id, groupB.Id, "Item B", FractionalIndex.Initial(), OwnerId, FixedTime));

        context.BoardFields.Add(BoardField.Create(AccountId, wsA, boardA.Id, "Field A", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Initial(), OwnerId, FixedTime));
        context.BoardFields.Add(BoardField.Create(AccountId, wsB, boardB.Id, "Field B", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Initial(), OwnerId, FixedTime));

        context.Pages.Add(Page.Create(AccountId, wsA, "Page A", OwnerId, FixedTime));
        context.Pages.Add(Page.Create(AccountId, wsB, "Page B", OwnerId, FixedTime));

        var targetA = ResourceRef.Create(ResourceKind.Create("work-management.board"), boardA.Id, wsA);
        var targetB = ResourceRef.Create(ResourceKind.Create("work-management.board"), boardB.Id, wsB);
        context.Comments.Add(Comment.Create(AccountId, wsA, targetA, "\"Comment A\"", OwnerId, FixedTime));
        context.Comments.Add(Comment.Create(AccountId, wsB, targetB, "\"Comment B\"", OwnerId, FixedTime));

        context.Forms.Add(Form.Create(AccountId, wsA, boardA.Id, "Form A", "form-a", OwnerId, FixedTime));
        context.Forms.Add(Form.Create(AccountId, wsB, boardB.Id, "Form B", "form-b", OwnerId, FixedTime));

        context.Spaces.Add(Space.Create(AccountId, wsA, "Space A", SpaceVisibility.Private, OwnerId, FixedTime));
        context.Spaces.Add(Space.Create(AccountId, wsB, "Space B", SpaceVisibility.Private, OwnerId, FixedTime));

        await context.SaveChangesAsync();

        tenant.SetWorkspace(AccountId, wsA, null);

        var boards = await context.Boards.ToListAsync();
        boards.Should().HaveCount(1);
        boards.Should().AllSatisfy(b => b.WorkspaceId.Should().Be(wsA));

        var items = await context.BoardItems.ToListAsync();
        items.Should().HaveCount(1);
        items.Should().AllSatisfy(i => i.WorkspaceId.Should().Be(wsA));

        var fields = await context.BoardFields.ToListAsync();
        fields.Should().HaveCount(1);
        fields.Should().AllSatisfy(f => f.WorkspaceId.Should().Be(wsA));

        var pages = await context.Pages.ToListAsync();
        pages.Should().HaveCount(1);
        pages.Should().AllSatisfy(p => p.WorkspaceId.Should().Be(wsA));

        var comments = await context.Comments.ToListAsync();
        comments.Should().HaveCount(1);
        comments.Should().AllSatisfy(c => c.WorkspaceId.Should().Be(wsA));

        var forms = await context.Forms.ToListAsync();
        forms.Should().HaveCount(1);
        forms.Should().AllSatisfy(f => f.WorkspaceId.Should().Be(wsA));

        var spaces = await context.Spaces.ToListAsync();
        spaces.Should().HaveCount(1);
        spaces.Should().AllSatisfy(s => s.WorkspaceId.Should().Be(wsA));
    }
}
