using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateFieldOption;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// An Item is done exactly when its status option (on the Board's Status field)
/// is in the done category. Changing an option's category therefore completes
/// or reopens every active Item holding that option, in the same transaction as
/// the category change, exactly as writing the status would; Items with another
/// status, archived Items, and Items holding the option of another Status field
/// are left alone.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class StatusCategoryCompletionIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public StatusCategoryCompletionIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ChangingAnOptionsCategory_CompletesAndReopensTheItemsHoldingIt()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var boardId = await _host.SeedBoardAsync(stack, "Delivery");
        var seed = await SeedAsync(stack, boardId);

        using var provider = _host.CreateProvider(stack.AccountId, stack.OwnerId);

        await SetCategoryAsync(provider, boardId, seed.StatusFieldId, seed.InProgressOptionId, "done");

        var afterDone = await CompletionAsync(seed);
        afterDone.Working.Should().NotBeNull("an Item whose status became a done status is completed");
        afterDone.Todo.Should().BeNull("an Item with another status is untouched");
        afterDone.ArchivedWorking.Should().BeNull("archived Items are not changed");
        afterDone.OtherField.Should().BeNull("only the Board's Status field decides completion");

        await SetCategoryAsync(provider, boardId, seed.StatusFieldId, seed.InProgressOptionId, "in_progress");

        (await CompletionAsync(seed)).Working.Should().BeNull("leaving the done category reopens the Item");
    }

    private sealed record Seed(
        Guid StatusFieldId, Guid InProgressOptionId,
        Guid WorkingItemId, Guid TodoItemId, Guid ArchivedWorkingItemId, Guid OtherFieldItemId);

    private async Task<Seed> SeedAsync(OrgStack stack, Guid boardId)
    {
        var now = OrgTreeTestHost.BaseTime;
        var settings = FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}"));
        var status = BoardField.Create(stack.AccountId, stack.WorkspaceId, boardId, "Status", FieldType.Status, settings, FractionalIndex.Create("a0"), stack.OwnerId, now, isSystem: true);
        status.AddDefaultStatusOptions(stack.OwnerId, now);
        var other = BoardField.Create(stack.AccountId, stack.WorkspaceId, boardId, "Review stage", FieldType.Status, settings, FractionalIndex.Create("a1"), stack.OwnerId, now);
        other.AddDefaultStatusOptions(stack.OwnerId, now);
        var inProgress = status.Options.Single(o => o.Category == WorkStatusCategory.InProgress);
        var todo = status.Options.Single(o => o.Category == WorkStatusCategory.Todo);
        var otherInProgress = other.Options.Single(o => o.Category == WorkStatusCategory.InProgress);

        var group = BoardGroup.Create(stack.AccountId, stack.WorkspaceId, boardId, "Group", Color.Create("#0079BF"), FractionalIndex.Create("a0"), stack.OwnerId, now);
        BoardItem Item(string title, string position, BoardField field, FieldOption option)
        {
            var item = BoardItem.CreateRoot(stack.AccountId, stack.WorkspaceId, boardId, group.Id, title, FractionalIndex.Create(position), stack.OwnerId, now);
            item.UpdateFieldValue(field, FieldValue.Create(JsonValue.Create($"\"{option.Id}\"")), stack.OwnerId, now);
            return item;
        }

        var working = Item("Working", "a0", status, inProgress);
        var todoItem = Item("Todo", "a1", status, todo);
        var archived = Item("Archived", "a2", status, inProgress);
        archived.Archive(stack.OwnerId, now);
        var otherItem = Item("Other field", "a3", other, otherInProgress);

        await _host.WithSystemContextAsync(c =>
        {
            c.BoardFields.AddRange(status, other);
            c.BoardGroups.Add(group);
            c.BoardItems.AddRange(working, todoItem, archived, otherItem);
            return Task.CompletedTask;
        });

        return new Seed(status.Id, inProgress.Id, working.Id, todoItem.Id, archived.Id, otherItem.Id);
    }

    private async Task SetCategoryAsync(Microsoft.Extensions.DependencyInjection.ServiceProvider provider, Guid boardId, Guid fieldId, Guid optionId, string category)
    {
        var (version, name, color) = await _host.ReadAsync(async c =>
        {
            var field = await c.BoardFields.Include(f => f.Options).SingleAsync(f => f.Id == fieldId);
            var option = field.Options.Single(o => o.Id == optionId);
            return (field.Version, option.Name, option.Color.Value);
        });
        await OrgTreeTestHost.SendIdempotentAsync(provider,
            new UpdateFieldOptionCommand(boardId, fieldId, optionId, name, color, version, category));
    }

    private Task<(DateTimeOffset? Working, DateTimeOffset? Todo, DateTimeOffset? ArchivedWorking, DateTimeOffset? OtherField)> CompletionAsync(Seed seed) =>
        _host.ReadAsync(async c =>
        {
            var completed = await c.BoardItems
                .Where(i => i.Id == seed.WorkingItemId || i.Id == seed.TodoItemId || i.Id == seed.ArchivedWorkingItemId || i.Id == seed.OtherFieldItemId)
                .ToDictionaryAsync(i => i.Id, i => i.CompletedAt);
            return (completed[seed.WorkingItemId], completed[seed.TodoItemId], completed[seed.ArchivedWorkingItemId], completed[seed.OtherFieldItemId]);
        });
}
