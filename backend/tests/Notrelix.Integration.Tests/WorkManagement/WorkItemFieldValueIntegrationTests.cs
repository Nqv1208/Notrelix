using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ClearFieldValue;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValue;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValues;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemStatus;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// ITEM-VALUE-001 (WM-V3-DOM-ITEM-001 / DOM-FIELD-001 / APP-ITEM-002). Field-value
/// writes run through the real handlers from a fresh context per request, as HTTP
/// requests do: the second write to the same field must update the one stored
/// value (the item's value collection is loaded), and option-backed values must
/// validate against the field's persisted options.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkItemFieldValueIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private Guid _accountId;
    private Guid _workspaceId;
    private Guid _userId;
    private Guid _itemId;
    private Guid _textFieldId;
    private Guid _selectFieldId;
    private Guid _optionId;
    private Guid _statusFieldId;
    private Guid _doneOptionId;
    private Guid _dueDateFieldId;

    public WorkItemFieldValueIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();
        _accountId = Guid.NewGuid();
        _userId = Guid.NewGuid();
        var workspace = Workspace.Create(_accountId, _userId, "Values WS", $"val-{Guid.NewGuid():N}", Now);
        var board = Board.Create(_accountId, workspace.Id, _userId, "Values Board", null, Now);
        var group = BoardGroup.Create(_accountId, workspace.Id, board.Id, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), _userId, Now);
        var item = BoardItem.CreateRoot(_accountId, workspace.Id, board.Id, group.Id, "Task", FractionalIndex.Create("a0"), _userId, Now);
        var text = BoardField.Create(_accountId, workspace.Id, board.Id, "Notes", FieldType.Text, FieldSettings.Empty(), FractionalIndex.Create("a0"), _userId, Now);
        var select = BoardField.Create(_accountId, workspace.Id, board.Id, "Priority", FieldType.Select, FieldSettings.Empty(), FractionalIndex.Create("a1"), _userId, Now);
        var option = select.AddOption("High", Color.Create("#FF0000"), FractionalIndex.Create("a0"), _userId, Now);
        // The Status field as the app creates it: a system field seeded with the default statuses.
        var status = BoardField.Create(_accountId, workspace.Id, board.Id, "Status", FieldType.Status,
            FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")), FractionalIndex.Create("a2"), _userId, Now, isSystem: true);
        status.AddDefaultStatusOptions(_userId, Now);
        (_workspaceId, _itemId, _textFieldId, _selectFieldId, _optionId) = (workspace.Id, item.Id, text.Id, select.Id, option.Id);
        (_statusFieldId, _doneOptionId) = (status.Id, status.Options.Single(o => o.Category == WorkStatusCategory.Done).Id);
        var dueDate = BoardField.Create(_accountId, workspace.Id, board.Id, "Due Date", FieldType.Date, FieldSettings.Empty(), FractionalIndex.Create("a3"), _userId, Now, isSystem: true);
        _dueDateFieldId = dueDate.Id;

        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Workspaces.Add(workspace);
        seed.Boards.Add(board);
        seed.BoardGroups.Add(group);
        seed.BoardItems.Add(item);
        seed.BoardFields.AddRange(text, select, status, dueDate);
        await seed.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SingleValue_WrittenTwiceFromFreshRequests_UpdatesTheOneStoredValue()
    {
        await SetValueAsync(_textFieldId, "first");
        await SetValueAsync(_textFieldId, "second");

        (await StoredValuesAsync(_textFieldId)).Should().ContainSingle().Which.Should().Contain("second");
    }

    [Fact]
    public async Task SelectValue_ValidatesAgainstPersistedOptions_AndClearsToNull()
    {
        await SetValueAsync(_selectFieldId, _optionId.ToString());
        (await StoredValuesAsync(_selectFieldId)).Should().ContainSingle().Which.Should().Contain(_optionId.ToString());

        await InTransactionAsync(context => new ClearFieldValueCommandHandler(context, CurrentUser(), Clock())
            .Handle(new ClearFieldValueCommand(_itemId, _selectFieldId, 1), CancellationToken.None));

        (await StoredValuesAsync(_selectFieldId)).Should().ContainSingle().Which.Should().Be("null");
    }

    [Fact]
    public async Task BulkValues_WrittenTwiceFromFreshRequests_UpdateInPlace()
    {
        await BulkAsync(new Dictionary<Guid, object?> { [_textFieldId] = "one" });
        await BulkAsync(new Dictionary<Guid, object?> { [_textFieldId] = "two" });

        (await StoredValuesAsync(_textFieldId)).Should().ContainSingle().Which.Should().Contain("two");
    }

    [Fact]
    public async Task StatusOfASystemStatusField_IsWrittenThroughTheStatusCommand()
    {
        await InTransactionAsync(context => new UpdateBoardItemStatusCommandHandler(context, CurrentUser(), Clock())
            .Handle(new UpdateBoardItemStatusCommand(_itemId, _doneOptionId.ToString(), ExpectedVersion: 1), CancellationToken.None));

        (await StoredValuesAsync(_statusFieldId)).Should().ContainSingle().Which.Should().Contain(_doneOptionId.ToString());
        (await ItemColumnAsync("completed_at")).Should().NotBeNull("a done-category status completes the Item");
    }

    [Fact]
    public async Task TheSystemDueDateField_WritesTheItemsDueAt_AndStoresNoValue()
    {
        await SetValueAsync(_dueDateFieldId, "2026-10-18T00:00:00.0000000+07:00");

        (await ItemColumnAsync("due_at")).Should().Be(new DateTimeOffset(2026, 10, 17, 17, 0, 0, TimeSpan.Zero));
        (await StoredValuesAsync(_dueDateFieldId)).Should().BeEmpty("BoardItem.DueAt is the authoritative due date");
    }

    private async Task<DateTimeOffset?> ItemColumnAsync(string column)
    {
        await using var connection = _db.CreateOwnerConnection();
        await connection.OpenAsync();
        await using var command = new Npgsql.NpgsqlCommand($"SELECT {column} FROM work.board_items WHERE id = '{_itemId}'", connection);
        var value = await command.ExecuteScalarAsync();
        return value is DateTime utc ? new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)) : value as DateTimeOffset?;
    }

    private Task SetValueAsync(Guid fieldId, object value) =>
        InTransactionAsync(context => new UpdateBoardItemFieldValueCommandHandler(context, CurrentUser(), Clock())
            .Handle(new UpdateBoardItemFieldValueCommand(_itemId, fieldId, value, 1), CancellationToken.None));

    private Task BulkAsync(Dictionary<Guid, object?> values) =>
        InTransactionAsync(context => new UpdateBoardItemFieldValuesCommandHandler(context, Request(), Clock(), Mock.Of<IResourceLocator>())
            .Handle(new UpdateBoardItemFieldValuesCommand(_itemId, values, 1), CancellationToken.None));

    private async Task<List<string>> StoredValuesAsync(Guid fieldId)
    {
        await using var connection = _db.CreateOwnerConnection();
        await connection.OpenAsync();
        await using var command = new Npgsql.NpgsqlCommand(
            $"SELECT value::text FROM work.board_item_values WHERE item_id = '{_itemId}' AND field_id = '{fieldId}'", connection);
        var values = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) values.Add(reader.GetString(0));
        return values;
    }

    private async Task InTransactionAsync<T>(Func<ApplicationDbContext, Task<T>> work)
    {
        await using var context = _db.CreateOwnerContext(Tenant());
        await using var transaction = await context.Database.BeginTransactionAsync();
        await work(context);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private ICurrentUser CurrentUser()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.UserId).Returns(_userId);
        return currentUser.Object;
    }

    private FakeCurrentRequestContext Request()
    {
        var request = new FakeCurrentRequestContext().AsUser(_userId);
        request.Tenant.SetWorkspace(_accountId, _workspaceId, _userId);
        return request;
    }

    private static IDateTimeProvider Clock() => FakeDateTimeProvider.WithFixedTime(Now);

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
