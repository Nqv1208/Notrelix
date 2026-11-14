using System.Text.Json;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data.WorkManagement;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// UC-WM-C17: the server's paged read sorts and filters the fixed dataset of
/// <c>backend/contracts/fixtures/field-type-parity.v1.json</c> to the result the web client gives on it
/// (<c>field-type-parity.unit.test.ts</c> reads the same file). A field the paged read cannot evaluate is refused,
/// never silently mismatched.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class PagedReadFieldTypeParityIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 3, 40, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private readonly JsonElement _fixture;
    private readonly Dictionary<string, Guid> _fieldIds = [];
    private readonly Dictionary<string, Guid> _optionIds = [];
    private readonly Dictionary<string, Guid> _people = [];
    private readonly Dictionary<Guid, string> _itemKeys = [];
    private readonly List<CriteriaField> _criteriaFields = [];
    private Guid _accountId;
    private Guid _workspaceId;
    private Guid _userId;
    private Guid _boardId;
    private Guid _groupId;

    public PagedReadFieldTypeParityIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _fixture = JsonDocument.Parse(File.ReadAllText(FixturePath())).RootElement;
    }

    public async Task InitializeAsync()
    {
        await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();
        _accountId = Guid.NewGuid();
        _userId = Guid.NewGuid();
        var workspace = Workspace.Create(_accountId, _userId, "Parity WS", $"parity-{Guid.NewGuid():N}", Now);
        _workspaceId = workspace.Id;
        var board = Board.Create(_accountId, workspace.Id, _userId, "Parity", null, Now, itemKeyPrefix: "PAR");
        _boardId = board.Id;
        var group = BoardGroup.Create(_accountId, workspace.Id, board.Id, "Group", Color.Create("#808080"), FractionalIndex.Create("a0"), _userId, Now);
        _groupId = group.Id;
        foreach (var person in _fixture.GetProperty("people").EnumerateArray()) _people[person.GetString()!] = Guid.NewGuid();

        var fields = new Dictionary<string, BoardField>();
        var position = 0;
        foreach (var f in _fixture.GetProperty("fields").EnumerateArray())
        {
            var key = f.GetProperty("key").GetString()!;
            var type = TypeOf(f.GetProperty("type").GetString()!);
            if (type is FieldType.Formula or FieldType.Rollup or FieldType.Vote)
            {
                // Not stored: the read only needs to know the field and its type to refuse it.
                var computedId = Guid.NewGuid();
                _fieldIds[key] = computedId;
                _criteriaFields.Add(new CriteriaField(computedId, type, false, false, PersonSource.Stored, []));
                continue;
            }

            var settings = type == FieldType.Person ? FieldSettings.Create(JsonValue.Create("{\"multiple\":true}"))
                : type == FieldType.Status ? FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}"))
                : FieldSettings.Empty();
            var field = BoardField.Create(_accountId, workspace.Id, board.Id, f.GetProperty("name").GetString()!, type, settings, FractionalIndex.Create($"a{position++}"), _userId, Now);
            var order = new List<Guid>();
            if (f.TryGetProperty("options", out var options))
            {
                var at = 0;
                foreach (var option in options.EnumerateArray())
                {
                    var added = field.AddOption(option.GetString()!, Color.Create("#808080"), FractionalIndex.Create($"a{at++}"), _userId, Now,
                        type == FieldType.Status ? WorkStatusCategory.Todo : null);
                    _optionIds[option.GetString()!] = added.Id;
                    order.Add(added.Id);
                }
            }

            fields[key] = field;
            _fieldIds[key] = field.Id;
            _criteriaFields.Add(new CriteriaField(field.Id, type, false, false, PersonSource.Stored, order));
        }

        var items = new List<BoardItem>();
        var place = 0;
        foreach (var entry in _fixture.GetProperty("items").EnumerateArray())
        {
            var (sequence, itemKey) = board.GenerateNextItemIdentity(_userId, Now);
            var item = BoardItem.CreateRoot(_accountId, workspace.Id, board.Id, group.Id, entry.GetProperty("key").GetString()!, FractionalIndex.Create($"b{place++:D2}"), _userId, Now, itemKey, sequence);
            foreach (var value in entry.GetProperty("values").EnumerateObject())
            {
                item.UpdateFieldValue(fields[value.Name], FieldValue.Create(JsonValue.Create(Stored(value.Value))), _userId, Now);
            }

            _itemKeys[item.Id] = entry.GetProperty("key").GetString()!;
            items.Add(item);
        }

        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Workspaces.Add(workspace);
        seed.Boards.Add(board);
        seed.BoardGroups.Add(group);
        seed.BoardFields.AddRange(fields.Values);
        seed.BoardItems.AddRange(items);
        await seed.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task EverySortOfTheFixture_GivesTheClientsOrder()
    {
        var failures = new List<string>();
        foreach (var c in _fixture.GetProperty("sorts").EnumerateArray())
        {
            var actual = await ReadAsync(BoardItemCriteria.Parse(null, SortOf(c.GetProperty("sort"))));
            var expected = c.GetProperty("expected").EnumerateArray().Select(e => e.GetString()!).ToList();
            if (!actual.SequenceEqual(expected)) failures.Add($"{c.GetProperty("name").GetString()}: [{string.Join(", ", actual)}] vs [{string.Join(", ", expected)}]");
        }

        failures.Should().BeEmpty();
    }

    [Fact]
    public async Task EveryFilterOfTheFixture_KeepsTheClientsItems()
    {
        var failures = new List<string>();
        foreach (var c in _fixture.GetProperty("filters").EnumerateArray())
        {
            var actual = await ReadAsync(BoardItemCriteria.Parse(FilterOf(c), null));
            var expected = c.GetProperty("expected").EnumerateArray().Select(e => e.GetString()!).ToList();
            if (!actual.SequenceEqual(expected)) failures.Add($"{c.GetProperty("name").GetString()}: [{string.Join(", ", actual)}] vs [{string.Join(", ", expected)}]");
        }

        failures.Should().BeEmpty();
    }

    [Fact]
    public async Task AFieldThePagedReadCannotEvaluate_IsRefused_NeverSilentlyMismatched()
    {
        foreach (var c in _fixture.GetProperty("serverRefuses").EnumerateArray())
        {
            var criteria = c.TryGetProperty("sort", out var sort)
                ? BoardItemCriteria.Parse(null, SortOf(sort))
                : BoardItemCriteria.Parse(FilterOf(c), null);
            var read = () => ReadAsync(criteria);
            await read.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ValidationException>(c.GetProperty("name").GetString());
        }
    }

    private async Task<List<string>> ReadAsync(BoardItemCriteria criteria)
    {
        await using var context = _db.CreateOwnerContext(Tenant());
        var lanes = await new PostgresBoardItemPageReader(context)
            .ReadAsync(_boardId, [_groupId], null, _criteriaFields, criteria, null, null, 200, CancellationToken.None);
        return lanes.SelectMany(l => l.ItemIds).Select(id => _itemKeys[id]).ToList();
    }

    private string SortOf(JsonElement levels) => string.Join("~", levels.EnumerateArray().Select(level =>
        $"{_fieldIds[level[0].GetString()!]}.{level[1].GetString()}"));

    private string FilterOf(JsonElement c) =>
        "c2:" + c.GetProperty("join").GetString() + string.Concat(c.GetProperty("conditions").EnumerateArray().Select(condition =>
        {
            var parts = condition.EnumerateArray().Select(p => p.GetString()!).ToList();
            var operands = parts.Skip(2).Select(Operand);
            return "~" + string.Join(".", new[] { _fieldIds[parts[0]].ToString(), parts[1] }.Concat(operands).Select(Uri.EscapeDataString));
        }));

    /// <summary>An option or person key becomes its id; any other operand (text, number, day, "__none__") stays.</summary>
    private string Operand(string value) =>
        _optionIds.TryGetValue(value, out var option) ? option.ToString()
        : _people.TryGetValue(value, out var person) ? person.ToString()
        : value;

    private string Stored(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Array => JsonSerializer.Serialize(value.EnumerateArray().Select(v => Operand(v.GetString()!)).ToList()),
        JsonValueKind.String => JsonSerializer.Serialize(Operand(value.GetString()!)),
        _ => value.GetRawText(),
    };

    private static FieldType TypeOf(string wire) => wire switch
    {
        "text" => FieldType.Text,
        "long_text" => FieldType.LongText,
        "link" => FieldType.Link,
        "number" => FieldType.Number,
        "date" => FieldType.Date,
        "checkbox" => FieldType.Checkbox,
        "select" => FieldType.Select,
        "status" => FieldType.Status,
        "multi_select" => FieldType.MultiSelect,
        "person" => FieldType.Person,
        "formula" => FieldType.Formula,
        "rollup" => FieldType.Rollup,
        "vote" => FieldType.Vote,
        _ => throw new InvalidOperationException($"Unknown field type '{wire}' in the parity fixture."),
    };

    private static string FixturePath()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "contracts", "fixtures", "field-type-parity.v1.json");
            if (File.Exists(candidate)) return candidate;
        }

        throw new FileNotFoundException("backend/contracts/fixtures/field-type-parity.v1.json was not found above the test binaries.");
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
