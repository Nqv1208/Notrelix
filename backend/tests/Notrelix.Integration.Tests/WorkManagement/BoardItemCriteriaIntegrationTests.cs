using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Labels;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data.WorkManagement;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// The criteria of a paged board on PostgreSQL through the real page reader: the operators of each field type, the
/// sort (empty last ascending, first descending, options by board order, accent-folded text), the counts before and
/// after the filter, the lanes, a condition or sort on a field the board no longer has (ignored), and the fields
/// and operators it refuses with a validation failure.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class BoardItemCriteriaIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 3, 40, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private Guid _accountId;
    private Guid _workspaceId;
    private Guid _userId;
    private readonly Guid _other = Guid.NewGuid();
    private Board _board = null!;
    private BoardGroup _group = null!;
    private BoardField _status = null!, _note = null!, _points = null!, _due = null!, _done = null!, _pick = null!, _tags = null!, _owner = null!;
    private Label _bug = null!;
    private readonly Dictionary<Guid, string> _names = [];

    // Items in board order: Alpha, Beta, Gamma, Delta (positions a0..a3).
    //            Note       Points Due         Done   Pick  Tags   Assignees      Lane
    // Alpha      Café đỏ    5      2026-11-01  true   Cao   x,y    me             todo
    // Beta       plain      10     2026-11-10  false  Thấp  y      other          in progress
    // Gamma      (none)     (none) (none)      (none) none  none   (none)         empty lane
    // Delta      CAFE       5      2026-11-05  true   Cao   x      me + other     todo
    public BoardItemCriteriaIntegrationTests(PostgresTestContainer db) => _db = db;

    public async Task InitializeAsync()
    {
        await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();
        _accountId = Guid.NewGuid();
        _userId = Guid.NewGuid();
        var workspace = Workspace.Create(_accountId, _userId, "Criteria WS", $"crit-{Guid.NewGuid():N}", Now);
        _workspaceId = workspace.Id;
        _board = Board.Create(_accountId, workspace.Id, _userId, "Criteria Board", null, Now, itemKeyPrefix: "CRT");
        _group = BoardGroup.Create(_accountId, workspace.Id, _board.Id, "Group", Color.Create("#808080"), FractionalIndex.Create("a0"), _userId, Now);
        _status = Field("Status", FieldType.Status, "a1");
        _status.AddDefaultStatusOptions(_userId, Now);
        _note = Field("Note", FieldType.Text, "a2");
        _points = Field("Points", FieldType.Number, "a3");
        _due = Field("Hạn", FieldType.Date, "a4");
        _done = Field("Done", FieldType.Checkbox, "a5");
        _pick = Field("Pick", FieldType.Select, "a6");
        _pick.AddOption("Cao", Color.Create("#E53935"), FractionalIndex.Create("a0"), _userId, Now);
        _pick.AddOption("Thấp", Color.Create("#43A047"), FractionalIndex.Create("a1"), _userId, Now);
        _tags = Field("Tags", FieldType.MultiSelect, "a7");
        _tags.AddOption("x", Color.Create("#E53935"), FractionalIndex.Create("a0"), _userId, Now);
        _tags.AddOption("y", Color.Create("#43A047"), FractionalIndex.Create("a1"), _userId, Now);
        _owner = Field("Owner", FieldType.Person, "a8");
        _bug = Label.Create(_accountId, workspace.Id, _board.Id, "Bug", LabelColor.Create("#E53935"), _userId, Now);

        var alpha = Item("Alpha", "a0");
        var beta = Item("Beta", "a1");
        var gamma = Item("Gamma", "a2");
        var delta = Item("Delta", "a3");
        Set(alpha, _status, Q(Category(WorkStatusCategory.Todo)));
        Set(delta, _status, Q(Category(WorkStatusCategory.Todo)));
        Set(beta, _status, Q(Category(WorkStatusCategory.InProgress)));
        Set(alpha, _note, "\"Café đỏ\""); Set(beta, _note, "\"plain\""); Set(delta, _note, "\"CAFE\"");
        Set(alpha, _points, "5"); Set(beta, _points, "10"); Set(delta, _points, "5");
        Set(alpha, _due, "\"2026-11-01T00:00:00Z\""); Set(beta, _due, "\"2026-11-10T00:00:00Z\""); Set(delta, _due, "\"2026-11-05T00:00:00Z\"");
        Set(alpha, _done, "true"); Set(beta, _done, "false"); Set(delta, _done, "true");
        Set(alpha, _pick, Q(Opt(_pick, "Cao"))); Set(beta, _pick, Q(Opt(_pick, "Thấp"))); Set(delta, _pick, Q(Opt(_pick, "Cao")));
        Set(alpha, _tags, $"[\"{Opt(_tags, "x")}\",\"{Opt(_tags, "y")}\"]"); Set(beta, _tags, $"[\"{Opt(_tags, "y")}\"]"); Set(delta, _tags, $"[\"{Opt(_tags, "x")}\"]");

        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Workspaces.Add(workspace);
        seed.Boards.Add(_board);
        seed.BoardGroups.Add(_group);
        seed.BoardFields.AddRange(_status, _note, _points, _due, _done, _pick, _tags, _owner);
        seed.BoardItems.AddRange(alpha, beta, gamma, delta);
        seed.Labels.Add(_bug);
        seed.BoardItemLabels.AddRange(
            BoardItemLabel.Create(_accountId, workspace.Id, _board.Id, alpha.Id, _bug.Id, _userId, Now),
            BoardItemLabel.Create(_accountId, workspace.Id, _board.Id, delta.Id, _bug.Id, _userId, Now));
        seed.BoardItemMembers.AddRange(
            BoardItemMember.Create(_accountId, workspace.Id, _board.Id, alpha.Id, _userId, _userId, Now),
            BoardItemMember.Create(_accountId, workspace.Id, _board.Id, beta.Id, _other, _userId, Now),
            BoardItemMember.Create(_accountId, workspace.Id, _board.Id, delta.Id, _userId, _userId, Now),
            BoardItemMember.Create(_accountId, workspace.Id, _board.Id, delta.Id, _other, _userId, Now));
        await seed.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("Note", "contains", "cafe", "Alpha,Delta")]
    [InlineData("Note", "contains", "đỏ", "Alpha")]
    [InlineData("Note", "not_contains", "cafe", "Beta,Gamma")]
    [InlineData("Note", "equals", "PLAIN", "Beta")]
    [InlineData("Note", "not_equals", "plain", "Alpha,Gamma,Delta")]
    [InlineData("Note", "is_empty", "", "Gamma")]
    [InlineData("Note", "is_not_empty", "", "Alpha,Beta,Delta")]
    [InlineData("Points", "equals", "5", "Alpha,Delta")]
    [InlineData("Points", "not_equals", "5", "Beta,Gamma")]
    [InlineData("Points", "gt", "5", "Beta")]
    [InlineData("Points", "gte", "5", "Alpha,Beta,Delta")]
    [InlineData("Points", "lt", "10", "Alpha,Delta")]
    [InlineData("Points", "lte", "10", "Alpha,Beta,Delta")]
    [InlineData("Points", "is_empty", "", "Gamma")]
    [InlineData("Points", "is_not_empty", "", "Alpha,Beta,Delta")]
    [InlineData("Points", "gt", "abc", "")]
    [InlineData("Hạn", "equals", "2026-11-05", "Delta")]
    [InlineData("Hạn", "not_equals", "2026-11-05", "Alpha,Beta,Gamma")]
    [InlineData("Hạn", "before", "2026-11-05", "Alpha")]
    [InlineData("Hạn", "after", "2026-11-05", "Beta")]
    [InlineData("Hạn", "is_empty", "", "Gamma")]
    [InlineData("Hạn", "is_not_empty", "", "Alpha,Beta,Delta")]
    [InlineData("Done", "is_true", "", "Alpha,Delta")]
    [InlineData("Done", "is_false", "", "Beta,Gamma")]
    public async Task Filter_ByFieldType_KeepsTheItemsTheOperatorMatches(string field, string op, string value, string expected)
    {
        var page = await ReadAsync(Filter(Cond(field, op, value)));

        Names(page).Should().Equal(Split(expected));
    }

    [Fact]
    public async Task Filter_DateBetween_KeepsTheDaysFromTo_AndAnIncompleteRangeMatchesNothing()
    {
        Names(await ReadAsync(Filter($"{Id("Hạn")}.between.2026-11-01.2026-11-05"))).Should().Equal("Alpha", "Delta");
        Names(await ReadAsync(Filter($"{Id("Hạn")}.between.2026-11-01"))).Should().BeEmpty();
    }

    [Theory]
    [InlineData("Pick", "equals", "Cao", "Alpha,Delta")]
    [InlineData("Pick", "not_equals", "Cao", "Beta,Gamma")]
    [InlineData("Pick", "has_any", "Thấp", "Beta")]
    [InlineData("Pick", "has_any", "Thấp|__none__", "Beta,Gamma")]
    [InlineData("Pick", "is_empty", "", "Gamma")]
    [InlineData("Pick", "is_not_empty", "", "Alpha,Beta,Delta")]
    [InlineData("Tags", "has_any", "x", "Alpha,Delta")]
    [InlineData("Tags", "has_all", "x|y", "Alpha")]
    [InlineData("Tags", "has_any", "x|y", "Alpha,Beta,Delta")]
    [InlineData("Tags", "is_empty", "", "Gamma")]
    [InlineData("Tags", "is_not_empty", "", "Alpha,Beta,Delta")]
    public async Task Filter_ByChoice_KeepsTheItemsHoldingTheOptions(string field, string op, string options, string expected)
    {
        var ids = options.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(o => o == "__none__" ? o : Opt(field == "Pick" ? _pick : _tags, o).ToString());
        var page = await ReadAsync(Filter($"{Id(field)}.{op}.{string.Join(".", ids)}"));

        Names(page).Should().Equal(Split(expected));
    }

    [Fact]
    public async Task Filter_ByStatus_KeepsTheItemsOfTheOption()
    {
        var page = await ReadAsync(Filter($"{Id("Status")}.equals.{Category(WorkStatusCategory.InProgress)}"));

        Names(page).Should().Equal("Beta");
    }

    [Theory]
    [InlineData("$assignee.has_any.ME", "Alpha,Delta")]
    [InlineData("$assignee.has_all.ME.OTHER", "Delta")]
    [InlineData("$assignee.is_empty", "Gamma")]
    [InlineData("$assignee.is_not_empty", "Alpha,Beta,Delta")]
    [InlineData("$assignee.has_any.__none__", "Gamma")]
    [InlineData("$label.has_any.BUG", "Alpha,Delta")]
    [InlineData("$label.is_empty", "Beta,Gamma")]
    [InlineData("$title.contains.ALPH", "Alpha")]
    [InlineData("$title.not_equals.alpha", "Beta,Gamma,Delta")]
    [InlineData("OWNER.has_any.OTHER", "Beta,Delta")]
    public async Task Filter_ByPeopleLabelAndTitle_ReadsTheItemsOwnPeopleAndLabels(string condition, string expected)
    {
        var resolved = condition.Replace("ME", _userId.ToString()).Replace("OTHER", _other.ToString()).Replace("BUG", _bug.Id.ToString())
            .Replace("OWNER", _owner.Id.ToString());
        var page = await ReadAsync(Filter(resolved), owner: PersonSource.Members);

        Names(page).Should().Equal(Split(expected));
    }

    [Fact]
    public async Task Filter_AnyOfSeveralConditions_UnionsThem_AndAllIntersects()
    {
        var any = $"c2:or~{Cond("Points", "gt", "5")}~{Cond("Done", "is_false", "")}";
        var all = $"c2:and~{Cond("Points", "equals", "5")}~{Cond("Note", "contains", "cafe")}";

        Names(await ReadAsync(any)).Should().Equal("Beta", "Gamma");
        Names(await ReadAsync(all)).Should().Equal("Alpha", "Delta");
    }

    [Fact]
    public async Task Filter_OnAFieldTheBoardNoLongerHas_IsIgnored()
    {
        var page = await ReadAsync($"c2:and~{Guid.NewGuid()}.equals.x~{Cond("Done", "is_true", "")}");

        Names(page).Should().Equal("Alpha", "Delta");
    }

    [Theory]
    [InlineData("Points", "contains", "5")]
    [InlineData("Done", "equals", "x")]
    [InlineData("Hạn", "contains", "x")]
    [InlineData("Pick", "gt", "x")]
    [InlineData("$overdue", "is_true", "")]
    public async Task Filter_WithAnOperatorOrFieldItCannotEvaluate_IsRefusedAndNeverMatchedSilently(string field, string op, string value)
    {
        var condition = field.StartsWith('$') ? $"{field}.{op}" : Cond(field, op, value);
        var act = () => ReadAsync(Filter(condition));

        (await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ValidationException>())
            .Which.Errors.Keys.Should().Contain("filter");
    }

    [Fact]
    public async Task Search_KeepsTheItemsWhoseTitleOrKeyHoldsTheText_IgnoringAccentsAndCase()
    {
        Names(await ReadAsync(null, search: "ALPHA")).Should().Equal("Alpha");
        Names(await ReadAsync(null, search: "crt-3")).Should().Equal("Gamma");
        Names(await ReadAsync(null, search: "nothing here")).Should().BeEmpty();
    }

    [Theory]
    [InlineData("Points.asc", "Alpha,Delta,Beta,Gamma")]
    [InlineData("Points.desc", "Gamma,Beta,Alpha,Delta")]
    [InlineData("Hạn.asc", "Alpha,Delta,Beta,Gamma")]
    [InlineData("Hạn.desc", "Gamma,Beta,Delta,Alpha")]
    [InlineData("Done.asc", "Beta,Alpha,Delta,Gamma")]
    [InlineData("Pick.asc", "Alpha,Delta,Beta,Gamma")]
    [InlineData("Pick.desc", "Gamma,Beta,Alpha,Delta")]
    [InlineData("$title.desc", "Gamma,Delta,Beta,Alpha")]
    [InlineData("Points.asc~Hạn.desc", "Delta,Alpha,Beta,Gamma")]
    public async Task Sort_PutsEmptyLastAscendingAndFirstDescending_TiesKeepBoardOrder(string sort, string expected)
    {
        var page = await ReadAsync(null, sort: Sort(sort));

        Names(page).Should().Equal(Split(expected));
    }

    [Theory]
    [InlineData("Tags.asc")]
    [InlineData("$label.asc")]
    [InlineData("$assignee.asc")]
    [InlineData("$overdue.asc")]
    public async Task Sort_ByAFieldItCannotOrder_IsRefused(string sort)
    {
        var act = () => ReadAsync(null, sort: Sort(sort));

        (await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ValidationException>())
            .Which.Errors.Keys.Should().Contain("sort");
    }

    [Fact]
    public async Task Sort_OnAFieldTheBoardNoLongerHas_IsIgnored()
    {
        Names(await ReadAsync(null, sort: $"{Guid.NewGuid()}.desc")).Should().Equal("Alpha", "Beta", "Gamma", "Delta");
    }

    [Fact]
    public async Task Counts_EachLaneKeepsItsOwnSize_AndTheFilteredSizeNarrowsIt()
    {
        var lanes = await ReadLanesAsync(Filter(Cond("Done", "is_true", "")));

        var todo = lanes.Single(l => l.LaneKey == Category(WorkStatusCategory.Todo).ToString());
        (todo.Total, todo.FilteredTotal).Should().Be((2, 2));
        var progress = lanes.Single(l => l.LaneKey == Category(WorkStatusCategory.InProgress).ToString());
        (progress.Total, progress.FilteredTotal).Should().Be((1, 0));
        var empty = lanes.Single(l => l.LaneKey == "");
        (empty.Total, empty.FilteredTotal).Should().Be((1, 0), "an Item with no status sits in the empty lane");
        lanes.Sum(l => l.Total).Should().Be(4);
        lanes.Sum(l => l.FilteredTotal).Should().Be(2);
    }

    [Fact]
    public async Task Paging_UnderASort_ContinuesFromTheCursor_WithoutRepeatOrGap_AndRefusesAnotherSortsCursor()
    {
        var sort = Sort("Points.desc");
        var todo = Category(WorkStatusCategory.Todo).ToString();
        var first = (await ReadLanesAsync(null, sort, onlyLane: todo, limit: 1)).Single();
        first.ItemIds.Select(i => _names[i]).Should().Equal("Alpha");
        first.NextCursor.Should().NotBeNull();
        first.FilteredTotal.Should().Be(2);

        var second = (await ReadLanesAsync(null, sort, onlyLane: todo, cursor: first.NextCursor, limit: 1)).Single();
        second.ItemIds.Select(i => _names[i]).Should().Equal("Delta");
        second.NextCursor.Should().BeNull();

        var act = () => ReadLanesAsync(null, Sort("Points.asc"), onlyLane: todo, cursor: first.NextCursor, limit: 1);
        (await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ValidationException>())
            .Which.Errors.Keys.Should().Contain("cursor");
    }

    private Task<IReadOnlyList<BoardItemLanePage>> ReadLanesAsync(
        string? filter, string? sort = null, string? search = null, string? onlyLane = null, string? cursor = null, int limit = 50, PersonSource owner = PersonSource.Stored, bool lanes = true) =>
        ReadAsyncCore(BoardItemCriteria.Parse(filter, sort, search), owner, onlyLane, cursor, limit, lanes);

    private async Task<IReadOnlyList<BoardItemLanePage>> ReadAsyncCore(BoardItemCriteria criteria, PersonSource owner, string? onlyLane, string? cursor, int limit, bool lanes)
    {
        await using var context = _db.CreateOwnerContext(Tenant());
        var all = new[] { _status, _note, _points, _due, _done, _pick, _tags, _owner };
        var fields = all.Select(f => new CriteriaField(
            f.Id, f.Type, f == _due, false, f == _owner ? owner : PersonSource.Stored,
            f.Options.OrderBy(o => o.Position).Select(o => o.Id).ToList())).ToList();
        return await new PostgresBoardItemPageReader(context).ReadAsync(
            _board.Id, [_group.Id], lanes ? fields.First(f => f.Id == _status.Id) : null, fields, criteria, onlyLane, cursor, limit, CancellationToken.None);
    }

    private async Task<IReadOnlyList<BoardItemLanePage>> ReadAsync(string? filter, string? sort = null, string? search = null, PersonSource owner = PersonSource.Stored) =>
        await ReadLanesAsync(filter, sort, search, owner: owner, lanes: false);

    /// <summary>Item names of the pages in read order (one lane unless a test asks for the status lanes).</summary>
    private List<string> Names(IReadOnlyList<BoardItemLanePage> lanes) => lanes.SelectMany(l => l.ItemIds).Select(i => _names[i]).ToList();

    private static string[] Split(string names) => names.Length == 0 ? [] : names.Split(',');

    private string Id(string name) => name switch
    {
        "Status" => _status.Id.ToString(),
        "Note" => _note.Id.ToString(),
        "Points" => _points.Id.ToString(),
        "Hạn" => _due.Id.ToString(),
        "Done" => _done.Id.ToString(),
        "Pick" => _pick.Id.ToString(),
        "Tags" => _tags.Id.ToString(),
        _ => throw new ArgumentException(name),
    };

    private string Cond(string field, string op, string value) =>
        $"{Id(field)}.{op}" + (value.Length == 0 ? "" : $".{Uri.EscapeDataString(value)}");

    private static string Filter(string condition) => $"c2:and~{condition}";

    private string Sort(string levels) => string.Join("~", levels.Split('~').Select(l =>
    {
        var parts = l.Split('.');
        return $"{(parts[0].StartsWith('$') ? Uri.EscapeDataString(parts[0]) : Id(parts[0]))}.{parts[1]}";
    }));

    private BoardField Field(string name, FieldType type, string position) =>
        BoardField.Create(_accountId, _workspaceId, _board.Id, name, type,
            type == FieldType.Status ? FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")) : FieldSettings.Empty(), FractionalIndex.Create(position), _userId, Now);

    private static string Q(Guid id) => $"\"{id}\"";

    private static Guid Opt(BoardField field, string name) => field.Options.Single(o => o.Name == name).Id;

    private Guid Category(WorkStatusCategory category) => _status.Options.Single(o => o.Category == category).Id;

    private BoardItem Item(string name, string position)
    {
        var (sequence, key) = _board.GenerateNextItemIdentity(_userId, Now);
        var item = BoardItem.CreateRoot(_accountId, _workspaceId, _board.Id, _group.Id, name, FractionalIndex.Create(position), _userId, Now, key, sequence);
        _names[item.Id] = name;
        return item;
    }

    private void Set(BoardItem item, BoardField field, string json) =>
        item.UpdateFieldValue(field, FieldValue.Create(JsonValue.Create(json)), _userId, Now);

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
