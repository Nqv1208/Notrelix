using Npgsql;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Checklists;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.WorkManagement;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// ORDER-DB-001 / ORDER-UNIQ-001 (WM-V3-DATA-001, WM-V3-DATA-002). Against the
/// migrated PostgreSQL schema: every P3 position column is ordinal text, database
/// ORDER BY equals Domain <see cref="FractionalIndex.CompareTo"/> for a boundary
/// corpus, the preflight grammar predicate equals the Domain grammar, and each of
/// the six named unique indexes rejects a duplicate active key while allowing the
/// lifecycle cases its filter excludes.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkOrderingSchemaIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 0, 0, 0, TimeSpan.Zero);

    private static readonly string[] Corpus =
    [
        "a0", "a1", "a9", "aA", "aZ", "aa", "az", "a0V", "a0v", "a0A", "a0z", "a00V", "Zz", "Zzz", "Y00", "b00", "b0z", "bA0", "bzz",
        "A" + new string('0', 25) + "1", "z" + new string('z', 26),
    ];

    private static readonly string[] InvalidKeys = ["", "a", "a10", "a 1", "a-1", "0a", "1", "aé", "A" + new string('0', 26), "b0", "a00"];

    private readonly PostgresTestContainer _db;
    private Workspace _workspace = null!;
    private Board _board = null!;
    private BoardGroup _group = null!;
    private BoardItem _item = null!;
    private BoardField _field = null!;
    private Checklist _checklist = null!;

    public WorkOrderingSchemaIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    private Guid AccountId => _workspace.AccountId;
    private Guid UserId { get; set; }

    public async Task InitializeAsync()
    {
        await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        UserId = userId;
        _workspace = Workspace.Create(accountId, userId, "Ordering WS", $"ord-{Guid.NewGuid():N}", Now);
        _board = Board.Create(accountId, _workspace.Id, userId, "Ordering Board", null, Now);
        _group = BoardGroup.Create(accountId, _workspace.Id, _board.Id, "Todo", Color.Create("#808080"), Key("a0"), userId, Now);
        _item = BoardItem.CreateRoot(accountId, _workspace.Id, _board.Id, _group.Id, "Task", Key("a0"), userId, Now);
        _field = BoardField.Create(accountId, _workspace.Id, _board.Id, "Priority", FieldType.Select, FieldSettings.Empty(), Key("a0"), userId, Now);
        _checklist = Checklist.Create(accountId, _workspace.Id, _item.Id, "Checklist", Key("a0"), userId, Now);

        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Workspaces.Add(_workspace);
        seed.Boards.Add(_board);
        seed.BoardGroups.Add(_group);
        seed.BoardItems.Add(_item);
        seed.BoardFields.Add(_field);
        seed.Checklists.Add(_checklist);
        await seed.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task EveryP3PositionColumn_IsOrdinalText()
    {
        var columns = await QueryAsync("""
            SELECT table_name || ':' || data_type || ':' || coalesce(collation_name, '<default>')
            FROM information_schema.columns
            WHERE table_schema = 'work' AND column_name = 'position'
              AND table_name IN ('board_items', 'board_groups', 'board_fields', 'checklists', 'checklist_items', 'field_options')
            ORDER BY table_name
            """);

        columns.Should().Equal(
            "board_fields:text:C", "board_groups:text:C", "board_items:text:C",
            "checklist_items:text:C", "checklists:text:C", "field_options:text:C");
    }

    [Fact]
    public async Task DatabaseOrderBy_EqualsDomainCompareTo_ForTheBoundaryCorpus()
    {
        await using (var context = _db.CreateOwnerContext(SystemTenant()))
        {
            var field = await context.BoardFields.Include(f => f.Options).SingleAsync(f => f.Id == _field.Id);
            for (var i = 0; i < Corpus.Length; i++)
                field.AddOption($"o{i}", Color.Create("#808080"), Key(Corpus[i]), UserId, Now);
            await context.SaveChangesAsync();
        }

        var databaseOrder = await QueryAsync($"SELECT position FROM work.field_options WHERE field_id = '{_field.Id}' ORDER BY position");
        var domainOrder = Corpus.Select(Key).Order().Select(k => k.Value).ToList();

        databaseOrder.Should().Equal(domainOrder, "uppercase sorts before lowercase and digits before letters in both (ordinal)");
    }

    [Fact]
    public async Task PreflightGrammarPredicate_AcceptsExactlyTheDomainGrammar()
    {
        var keys = Corpus.Concat(InvalidKeys).ToList();
        var values = string.Join(", ", keys.Select(k => $"('{k}')"));

        var accepted = await QueryAsync($"SELECT position FROM (VALUES {values}) AS v(position) WHERE {WorkOrderingPreflight.ValidKeyPredicate}");

        var domainAccepted = keys.Where(k =>
        {
            try { FractionalIndex.Create(k); return true; }
            catch (Exception) { return false; }
        });
        accepted.Should().BeEquivalentTo(domainAccepted);
        InvalidKeys.Should().NotIntersectWith(accepted, "the corpus must exercise rejection");
    }

    [Fact]
    public async Task EachUniqueIndex_RejectsADuplicateActiveKey_WithItsConstraintName()
    {
        var rejected = new Dictionary<string, string?>
        {
            [WorkOrderingConstraints.GroupItems] = await DuplicateAsync(c => c.BoardItems.Add(
                BoardItem.CreateRoot(AccountId, _workspace.Id, _board.Id, _group.Id, "Dup", Key("a0"), UserId, Now))),
            [WorkOrderingConstraints.BoardGroups] = await DuplicateAsync(c => c.BoardGroups.Add(
                BoardGroup.Create(AccountId, _workspace.Id, _board.Id, "Dup", Color.Create("#808080"), Key("a0"), UserId, Now))),
            [WorkOrderingConstraints.BoardFields] = await DuplicateAsync(c => c.BoardFields.Add(
                BoardField.Create(AccountId, _workspace.Id, _board.Id, "Dup", FieldType.Text, FieldSettings.Empty(), Key("a0"), UserId, Now))),
            [WorkOrderingConstraints.ItemChecklists] = await DuplicateAsync(c => c.Checklists.Add(
                Checklist.Create(AccountId, _workspace.Id, _item.Id, "Dup", Key("a0"), UserId, Now))),
            [WorkOrderingConstraints.ChecklistItems] = await DuplicateAsync(async c =>
            {
                var checklist = await c.Checklists.Include(x => x.Items).SingleAsync(x => x.Id == _checklist.Id);
                checklist.AddItem("One", Key("a5"), UserId, Now);
                checklist.AddItem("Two", Key("a5"), UserId, Now);
            }),
            [WorkOrderingConstraints.FieldOptions] = await DuplicateAsync(async c =>
            {
                var field = await c.BoardFields.Include(x => x.Options).SingleAsync(x => x.Id == _field.Id);
                field.AddOption("One", Color.Create("#808080"), Key("a5"), UserId, Now);
                field.AddOption("Two", Color.Create("#808080"), Key("a5"), UserId, Now);
            }),
        };

        rejected.Should().AllSatisfy(pair => pair.Value.Should().Be(pair.Key));
    }

    [Fact]
    public async Task SoftDeletedAndChildItems_DoNotHoldTheActiveRootKey()
    {
        var active = BoardItem.CreateRoot(AccountId, _workspace.Id, _board.Id, _group.Id, "Active", Key("a5"), UserId, Now);
        await using (var context = _db.CreateOwnerContext(SystemTenant()))
        {
            var deleted = BoardItem.CreateRoot(AccountId, _workspace.Id, _board.Id, _group.Id, "Deleted", Key("a5"), UserId, Now);
            deleted.Delete(UserId, Now);
            context.BoardItems.Add(deleted);
            context.BoardItems.Add(active);
            await context.SaveChangesAsync();
        }

        // A child item (outside the P3 root scope, SPEC WM-V3-DEC-016) may share a root key.
        var child = await QueryAsync($"""
            UPDATE work.board_items SET parent_item_id = '{_item.Id}', position = 'a5'
            WHERE id = '{active.Id}' RETURNING id::text
            """);

        child.Should().ContainSingle();
    }

    private async Task<string?> DuplicateAsync(Func<ApplicationDbContext, Task> arrange)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        await arrange(context);
        try
        {
            await context.SaveChangesAsync();
            return null;
        }
        catch (DbUpdateException ex)
        {
            WorkOrderingConstraints.IsOrderingViolation(ex).Should().BeTrue("the violation maps to work.ordering.conflict by name");
            return (ex.InnerException as PostgresException)?.ConstraintName;
        }
    }

    private Task<string?> DuplicateAsync(Action<ApplicationDbContext> arrange) =>
        DuplicateAsync(context => { arrange(context); return Task.CompletedTask; });

    private async Task<List<string>> QueryAsync(string sql)
    {
        await using var connection = _db.CreateOwnerConnection();
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        var values = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) values.Add(reader.GetString(0));
        return values;
    }

    private static FractionalIndex Key(string value) => FractionalIndex.Create(value);

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }
}
