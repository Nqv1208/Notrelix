using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetBoardItem;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetFullBoard;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Ports.Collaboration;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// The board read contract on PostgreSQL through the real query handlers: the full
/// board lists only non-archived root Items as group items, a parent's progress
/// counts its non-archived children and the done ones by status category, and the
/// item key, status and priority option ids and dates round-trip on both the
/// summary and the detail; field options carry their status category; the Space
/// id is shown only when the caller can see that Space.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class BoardReadContractIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 3, 40, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;
    private Guid _accountId;
    private Guid _workspaceId;
    private Guid _userId;
    private Guid _spaceId;
    private Board _board = null!;
    private BoardGroup _group = null!;
    private BoardField _status = null!;
    private BoardField _priority = null!;
    private BoardItem _parent = null!;
    private BoardItem _doneChild = null!;
    private BoardItem _todoChild = null!;
    private BoardItem _archivedChild = null!;
    private BoardItem _archivedRoot = null!;
    private BoardItem _plain = null!;

    public BoardReadContractIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.OwnerConnectionString);
        await _reset.ResetAsync();

        _accountId = Guid.NewGuid();
        _userId = Guid.NewGuid();
        _spaceId = Guid.NewGuid();
        var workspace = Workspace.Create(_accountId, _userId, "Read WS", $"read-{Guid.NewGuid():N}", Now);
        _workspaceId = workspace.Id;
        _board = Board.Create(_accountId, workspace.Id, _userId, "Read Board", null, Now, itemKeyPrefix: "WEB", spaceId: _spaceId);
        _group = BoardGroup.Create(_accountId, workspace.Id, _board.Id, "Group", Color.Create("#808080"), FractionalIndex.Create("a0"), _userId, Now);
        _status = BoardField.Create(_accountId, workspace.Id, _board.Id, "Status", FieldType.Status,
            FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")), FractionalIndex.Create("a1"), _userId, Now);
        _status.AddDefaultStatusOptions(_userId, Now);
        _priority = BoardField.Create(_accountId, workspace.Id, _board.Id, "Mức ưu tiên", FieldType.Select,
            FieldSettings.Empty(), FractionalIndex.Create("a2"), _userId, Now);
        _priority.AddOption("Cao", Color.Create("#E53935"), FractionalIndex.Create("a0"), _userId, Now);

        _parent = Root("Parent", "a0");
        _parent.SetTimeline(Now.AddDays(-1), Now.AddDays(6), _userId, Now);
        SetOption(_parent, _status, Category(WorkStatusCategory.InProgress));
        SetOption(_parent, _priority, _priority.Options.Single().Id);
        _doneChild = Child("Done child", "a1");
        SetOption(_doneChild, _status, Category(WorkStatusCategory.Done));
        _doneChild.Complete(Now, _userId, Now);
        _todoChild = Child("Todo child", "a2");
        SetOption(_todoChild, _status, Category(WorkStatusCategory.Todo));
        _archivedChild = Child("Archived child", "a3");
        SetOption(_archivedChild, _status, Category(WorkStatusCategory.Done));
        _archivedChild.Archive(_userId, Now);
        _archivedRoot = Root("Archived root", "a4");
        _archivedRoot.Archive(_userId, Now);
        _plain = Root("Plain", "a5");

        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Workspaces.Add(workspace);
        seed.Boards.Add(_board);
        seed.BoardGroups.Add(_group);
        seed.BoardFields.AddRange(_status, _priority);
        seed.BoardItems.AddRange(_parent, _doneChild, _todoChild, _archivedChild, _archivedRoot, _plain);
        await seed.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task FullBoard_ListsOnlyNonArchivedRootItems_WithTheirValuesAndChildProgress()
    {
        var board = await FullBoardAsync(visibleSpaces: [_spaceId]);

        board.SpaceId.Should().Be(_spaceId);
        board.ItemKeyPrefix.Should().Be("WEB");
        var items = board.Lists.Single(l => l.Id == _group.Id).BoardItems;
        items.Select(i => i.Name).Should().Equal(["Parent", "Plain"], "archived Items and child Items are not group items");

        var parent = items.Single(i => i.Id == _parent.Id);
        parent.ItemKey.Should().Be("WEB-1");
        parent.ParentItemId.Should().BeNull();
        parent.StatusOptionId.Should().Be(Category(WorkStatusCategory.InProgress));
        parent.PriorityOptionId.Should().Be(_priority.Options.Single().Id);
        parent.DueAt.Should().Be(Now.AddDays(6));
        parent.StartedAt.Should().Be(Now.AddDays(-1));
        parent.CompletedAt.Should().BeNull();
        parent.ChildProgress.Should().Be(new BoardItemChildProgressDto(1, 2), "two non-archived children, one in the done category");
        parent.Version.Should().Be(_parent.Version);

        var plain = items.Single(i => i.Id == _plain.Id);
        plain.ItemKey.Should().Be("WEB-6");
        plain.StatusOptionId.Should().BeNull();
        plain.PriorityOptionId.Should().BeNull();
        plain.ChildProgress.Should().BeNull("an Item without child Items has no derived progress");
    }

    [Fact]
    public async Task FullBoard_FieldOptionsCarryTheirStatusCategory()
    {
        var board = await FullBoardAsync(visibleSpaces: [_spaceId]);

        var status = board.Columns.Single(c => c.Id == _status.Id);
        status.Options!.OrderBy(o => o.Position, StringComparer.Ordinal).Select(o => (o.Name, o.Category)).Should().Equal(
            ("Cần làm", "todo"), ("Đang làm", "in_progress"), ("Chờ duyệt", "review"), ("Hoàn thành", "done"));
        var priority = board.Columns.Single(c => c.Id == _priority.Id);
        priority.Options!.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new { Id = _priority.Options.Single().Id, Name = "Cao", Color = "#E53935", Category = (string?)null });
    }

    [Fact]
    public async Task FullBoard_CountsTheLiveItemsHoldingAValueOfEachField()
    {
        var board = await FullBoardAsync(visibleSpaces: [_spaceId]);

        // The parent and its two live children; the archived child does not count.
        board.Columns.Single(c => c.Id == _status.Id).ValueCount.Should().Be(3);
        board.Columns.Single(c => c.Id == _priority.Id).ValueCount.Should().Be(1);
    }

    [Fact]
    public async Task FullBoard_HidesASpaceTheCallerCannotSee()
    {
        var board = await FullBoardAsync(visibleSpaces: []);

        board.SpaceId.Should().BeNull("a Space the caller cannot see is never disclosed");
    }

    [Fact]
    public async Task ItemDetail_CarriesTheSameValues_ForAParentAndAChild()
    {
        var parent = await ItemAsync(_parent.Id);
        parent.ItemKey.Should().Be("WEB-1");
        parent.StatusOptionId.Should().Be(Category(WorkStatusCategory.InProgress));
        parent.PriorityOptionId.Should().Be(_priority.Options.Single().Id);
        parent.DueAt.Should().Be(Now.AddDays(6));
        parent.ChildProgress.Should().Be(new BoardItemChildProgressDto(1, 2));

        var child = await ItemAsync(_doneChild.Id);
        child.ItemKey.Should().Be("WEB-2");
        child.ParentItemId.Should().Be(_parent.Id);
        child.StatusOptionId.Should().Be(Category(WorkStatusCategory.Done));
        child.CompletedAt.Should().Be(Now);
        child.ChildProgress.Should().BeNull();
    }

    private async Task<FullBoardDto> FullBoardAsync(IReadOnlyList<Guid> visibleSpaces)
    {
        var visibility = new Mock<IResourceVisibility>();
        visibility.Setup(v => v.VisibleSpaceIdsAsync(_workspaceId, _userId, It.IsAny<CancellationToken>())).ReturnsAsync(visibleSpaces);
        await using var context = _db.CreateOwnerContext(Tenant());
        var result = await new GetFullBoardQueryHandler(context, Actors(), Collaboration(), visibility.Object, Request())
            .Handle(new GetFullBoardQuery(_board.Id), CancellationToken.None);
        result.Succeeded.Should().BeTrue();
        return result.Data!;
    }

    private async Task<BoardItemDto> ItemAsync(Guid itemId)
    {
        await using var context = _db.CreateOwnerContext(Tenant());
        var result = await new GetBoardItemQueryHandler(context, Actors(), Collaboration())
            .Handle(new GetBoardItemQuery(itemId), CancellationToken.None);
        result.Succeeded.Should().BeTrue();
        return result.Data!;
    }

    private Guid Category(WorkStatusCategory category) => _status.Options.Single(o => o.Category == category).Id;

    private BoardItem Root(string name, string position)
    {
        var (sequence, key) = _board.GenerateNextItemIdentity(_userId, Now);
        return BoardItem.CreateRoot(_accountId, _workspaceId, _board.Id, _group.Id, name, FractionalIndex.Create(position), _userId, Now, key, sequence);
    }

    private BoardItem Child(string name, string position)
    {
        var (sequence, key) = _board.GenerateNextItemIdentity(_userId, Now);
        var path = ItemParentPath.Create(_accountId, _workspaceId, _board.Id, _parent.Id, 0, []);
        return BoardItem.CreateChild(path, _group.Id, name, FractionalIndex.Create(position), _userId, Now, key, sequence);
    }

    private void SetOption(BoardItem item, BoardField field, Guid optionId) =>
        item.UpdateFieldValue(field, FieldValue.Create(JsonValue.Create($"\"{optionId}\"")), _userId, Now);

    private static IActorLookupService Actors()
    {
        var actors = new Mock<IActorLookupService>();
        actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ActorSnapshot>());
        return actors.Object;
    }

    private static IWorkManagementCollaborationReadPort Collaboration()
    {
        var port = new Mock<IWorkManagementCollaborationReadPort>();
        port.Setup(p => p.GetCountsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                (IReadOnlyDictionary<Guid, WorkItemCollaborationCounts>)ids.ToDictionary(id => id, _ => new WorkItemCollaborationCounts(0, 0)));
        return port.Object;
    }

    private FakeCurrentRequestContext Request()
    {
        var request = new FakeCurrentRequestContext().AsUser(_userId);
        request.Tenant.SetWorkspace(_accountId, _workspaceId, _userId);
        return request;
    }

    private ICurrentTenantContext Tenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_accountId, _workspaceId, _userId);
        return tenant;
    }

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }
}
