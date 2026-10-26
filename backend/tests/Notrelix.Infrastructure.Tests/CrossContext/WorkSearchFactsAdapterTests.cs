using Microsoft.EntityFrameworkCore;
using Notrelix.Application.Common.Context;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Infrastructure.CrossContext.Workspaces.WorkManagement;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Projections.Search;

namespace Notrelix.Infrastructure.Tests.CrossContext;

/// <summary>
/// The Work Management search facts behind the command search (ADR-010) over EF's in-memory provider: matching
/// is by the folded pattern, and an Item or Board outside the readable Board ids, in another Workspace, archived
/// or deleted is never returned or counted.
/// </summary>
public sealed class WorkSearchFactsAdapterTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);
    private readonly Guid _account = Guid.CreateVersion7();
    private readonly Guid _workspace = Guid.CreateVersion7();
    private readonly Guid _me = Guid.CreateVersion7();
    private readonly Guid _colleague = Guid.CreateVersion7();
    private readonly ApplicationDbContext _context = CreateContext();
    private int _position;

    public void Dispose() => _context.Dispose();

    private WorkSearchFactsAdapter Adapter => new(_context);

    private Board AddBoard(string title, Guid? workspace = null, Guid? space = null)
    {
        var board = Board.Create(_account, workspace ?? _workspace, _me, title, null, Now, BoardVisibility.Workspace);
        if (space is not null) board.PlaceInSpace(space, _me, Now);
        _context.Boards.Add(board);
        return board;
    }

    private BoardItem AddItem(Board board, string name, string? key = null, DateTimeOffset? due = null, Guid? workspace = null)
    {
        var item = BoardItem.CreateRoot(
            _account, workspace ?? board.WorkspaceId, board.Id, Guid.CreateVersion7(), name,
            FractionalIndex.Create($"a{_position++}"), _me, Now, itemKey: key, dueAt: due);
        _context.BoardItems.Add(item);
        return item;
    }

    private BoardItem AddSubItem(BoardItem parent, string name)
    {
        var item = BoardItem.CreateSubItem(parent, name, FractionalIndex.Create($"a{_position++}"), _me, Now);
        _context.BoardItems.Add(item);
        return item;
    }

    private void Assign(BoardItem item, Guid user, DateTimeOffset? at = null) =>
        _context.BoardItemMembers.Add(BoardItemMember.Create(_account, item.WorkspaceId, item.BoardId, item.Id, user, _me, at ?? Now));

    private Task<Notrelix.Application.Features.WorkManagement.Public.CommandSearch.WorkSearchResult> Search(
        string text, int items = 6, int boards = 5, params Board[] readable)
    {
        _context.SaveChanges();
        return Adapter.SearchAsync(_workspace, _me, readable.Select(b => b.Id).ToList(), text, items, boards, CancellationToken.None);
    }

    [Fact]
    public async Task Search_IgnoresMarksAndCase_OnTheItemName()
    {
        var board = AddBoard("Kế toán");
        var invoice = AddItem(board, "Hóa đơn tháng 10");
        AddItem(board, "Họp tuần");

        var result = await Search("hoa DON", readable: board);

        result.Items.Should().ContainSingle(i => i.Id == invoice.Id && i.ChildMatch == null && i.BoardName == "Kế toán");
    }

    [Fact]
    public async Task Search_MatchesTheItemKey()
    {
        var board = AddBoard("ERP");
        var keyed = AddItem(board, "Chuyển dữ liệu", key: "ERP-142");
        AddItem(board, "Khác", key: "ERP-9");

        var result = await Search("erp-14", readable: board);

        result.Items.Should().ContainSingle(i => i.Id == keyed.Id && i.Key == "ERP-142");
    }

    [Fact]
    public async Task Search_TypedTextIsNeverAPattern()
    {
        var board = AddBoard("Anything");
        AddItem(board, "Plain name");
        AddItem(board, "Cost (a+b) [draft]");

        (await Search(".*", readable: board)).Items.Should().BeEmpty("a typed dot-star is text, not a wildcard");
        (await Search("(a+b)", readable: board)).Items.Should().ContainSingle();
        (await Search("[draft]", readable: board)).Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Search_ShortOrEmptyText_AnswersEmpty()
    {
        var board = AddBoard("A");
        AddItem(board, "Alpha");

        (await Search("a", readable: board)).Items.Should().BeEmpty();
        (await Search("  ", readable: board)).Boards.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_NeverReturnsOrCountsWhatIsOutsideTheReadableBoards()
    {
        var readable = AddBoard("Open");
        var hidden = AddBoard("Secret budget board");
        AddItem(readable, "Budget plan");
        AddItem(hidden, "Budget for the secret project");
        AddItem(hidden, "Budget approval");

        var result = await Search("budget", readable: readable);

        result.Items.Should().ContainSingle(i => i.BoardId == readable.Id);
        result.Boards.Should().BeEmpty("the hidden board matches by name and holds two items, but is not readable");
    }

    [Fact]
    public async Task Search_LeavesOutArchivedAndDeletedItems_AndOtherWorkspaces()
    {
        var board = AddBoard("Work");
        var live = AddItem(board, "Report live");
        var archived = AddItem(board, "Report archived");
        archived.Archive(_me, Now);
        var deleted = AddItem(board, "Report deleted");
        deleted.Delete(_me, Now);
        var otherWorkspace = Guid.CreateVersion7();
        var foreign = AddBoard("Report board", otherWorkspace);
        AddItem(foreign, "Report foreign", workspace: otherWorkspace);

        var result = await Search("report", readable: [board, foreign]);

        result.Items.Should().ContainSingle(i => i.Id == live.Id);
        result.Boards.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_PutsTheCallersOwnItemsFirst_AndNamesAnotherPersonInCharge()
    {
        var board = AddBoard("Work");
        var others = AddItem(board, "Review draft A", due: Now.AddDays(1));
        var mine = AddItem(board, "Review draft B", due: Now.AddDays(9));
        Assign(others, _colleague);
        Assign(mine, _me);
        Assign(mine, _colleague, Now.AddMinutes(1));

        var result = await Search("review", readable: board);

        result.Items.Select(i => i.Id).Should().Equal(mine.Id, others.Id);
        result.Items[0].Mine.Should().BeTrue();
        result.Items[0].OtherAssigneeUserId.Should().Be(_colleague);
        result.Items[1].Mine.Should().BeFalse();
        result.Items[1].OtherAssigneeUserId.Should().Be(_colleague);
    }

    [Fact]
    public async Task Search_AMatchingSubItemStandsForItsParent_OnlyOnce()
    {
        var board = AddBoard("Work");
        var parent = AddItem(board, "Prepare launch");
        AddSubItem(parent, "Write the invoice template for finance");
        AddSubItem(parent, "Invoice review");
        var titled = AddItem(board, "Invoice archive");
        AddSubItem(titled, "Invoice backups");

        var result = await Search("invoice", readable: board);

        result.Items.Should().HaveCount(2);
        result.Items.Single(i => i.Id == titled.Id).ChildMatch.Should().BeNull("the Item's own name matched");
        var viaChild = result.Items.Single(i => i.Id == parent.Id);
        viaChild.ChildMatch.Should().NotBeNullOrEmpty().And.Subject.Length.Should().BeLessThanOrEqualTo(24);
        result.Items.Should().OnlyContain(i => i.Id == parent.Id || i.Id == titled.Id, "a sub-item is never a result of its own");
    }

    [Fact]
    public async Task Search_CutsToTheItemLimit_AfterRanking()
    {
        var board = AddBoard("Work");
        for (var i = 0; i < 8; i++) AddItem(board, $"Ticket {i}", due: Now.AddDays(i + 1));

        var result = await Search("ticket", items: 6, readable: board);

        result.Items.Should().HaveCount(6);
        result.Items.Select(i => i.Name).Should().Equal(Enumerable.Range(0, 6).Select(i => $"Ticket {i}"));
    }

    [Fact]
    public async Task Search_ABoardIsAResultByNameOrWithTwoMatchingItems_WithItsCount()
    {
        var space = Guid.CreateVersion7();
        var named = AddBoard("Hóa đơn board", space: space);
        var crowded = AddBoard("Operations");
        var single = AddBoard("Admin");
        AddItem(crowded, "Invoice one");
        AddItem(crowded, "Invoice two");
        AddItem(single, "Invoice only");

        var result = await Search("hoa don", readable: [named, crowded, single]);
        result.Boards.Should().ContainSingle().Which.Should().Match<Notrelix.Application.Features.WorkManagement.Public.CommandSearch.WorkSearchBoard>(
            b => b.Id == named.Id && b.SpaceId == space && b.MatchedItems == 0);

        var byItems = await Search("invoice", readable: [named, crowded, single]);
        byItems.Boards.Should().ContainSingle().Which.Should().Match<Notrelix.Application.Features.WorkManagement.Public.CommandSearch.WorkSearchBoard>(
            b => b.Id == crowded.Id && b.MatchedItems == 2);
        byItems.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task Search_ReadsTheStatusAndItsCategory()
    {
        var board = AddBoard("Work");
        var field = BoardField.Create(
            _account, _workspace, board.Id, "Status", FieldType.Status,
            FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")!),
            FractionalIndex.Create("a1"), _me, Now, isSystem: true);
        field.AddDefaultStatusOptions(_me, Now);
        _context.BoardFields.Add(field);
        var doing = field.Options.First(o => o.Category == WorkStatusCategory.InProgress);
        var item = AddItem(board, "Ship feature");
        AddItem(board, "Ship docs");
        _context.BoardItemValues.Add(BoardItemValue.Create(item.Id, field.Id, FieldValue.Create(JsonValue.Create($"\"{doing.Id}\"")!)));

        var result = await Search("ship", readable: board);

        var found = result.Items.Single(i => i.Id == item.Id);
        found.StatusName.Should().Be(doing.Name);
        found.StatusCategory.Should().Be("in_progress");
        result.Items.Single(i => i.Id != item.Id).StatusName.Should().BeNull();
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        // The system context lifts the tenant filter, so these tests prove the adapter's own Workspace scoping.
        var tenant = new Notrelix.Testing.Application.Fakes.FakeCurrentTenantContext();
        tenant.SetSystem();
        return new SearchTestContext(options, tenant);
    }

    private sealed class SearchTestContext(DbContextOptions<ApplicationDbContext> options, ICurrentTenantContext tenant)
        : ApplicationDbContext(options, tenant)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Ignore<SearchDocumentRecord>();
        }
    }
}
