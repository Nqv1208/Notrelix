using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.CreateBoardField;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValue;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Microsoft.EntityFrameworkCore;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// A Relation field through the real pipeline on PostgreSQL (UC-WM-C18, D78.6): the target board must be of the same
/// Workspace and readable, a link must name a live item of that board, and a refused link stores nothing.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class RelationFieldIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public RelationFieldIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ALinkToALiveItemOfTheTargetBoard_IsStored()
    {
        var scene = await SeedAsync();
        using var owner = _host.CreateProvider(scene.Stack.AccountId, scene.Stack.OwnerId);

        await OrgTreeTestHost.SendAsync(owner, new UpdateBoardItemFieldValueCommand(scene.SourceItemId, scene.FieldId, new[] { scene.TargetItemId }, await VersionAsync(scene.SourceItemId)));

        (await StoredAsync(scene.SourceItemId, scene.FieldId)).Should().Contain(scene.TargetItemId.ToString());
    }

    [Fact]
    public async Task ALinkToAnItemOfAnotherBoard_IsRefusedAndStoresNothing()
    {
        var scene = await SeedAsync();
        using var owner = _host.CreateProvider(scene.Stack.AccountId, scene.Stack.OwnerId);
        var foreign = await SeedItemAsync(scene.Stack, scene.OtherBoardId, "Elsewhere");

        var version = await VersionAsync(scene.SourceItemId);
        Func<Task> act = () => OrgTreeTestHost.SendAsync(owner, new UpdateBoardItemFieldValueCommand(scene.SourceItemId, scene.FieldId, new[] { foreign }, version));

        await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        (await StoredAsync(scene.SourceItemId, scene.FieldId)).Should().BeNull();
    }

    [Fact]
    public async Task ALinkToATrashedItem_IsRefused()
    {
        var scene = await SeedAsync();
        using var owner = _host.CreateProvider(scene.Stack.AccountId, scene.Stack.OwnerId);
        await _host.WithSystemContextAsync(c =>
        {
            var item = c.BoardItems.Single(i => i.Id == scene.TargetItemId);
            item.Delete(scene.Stack.OwnerId, OrgTreeTestHost.BaseTime);
            return Task.CompletedTask;
        });

        var version = await VersionAsync(scene.SourceItemId);
        Func<Task> act = () => OrgTreeTestHost.SendAsync(owner, new UpdateBoardItemFieldValueCommand(scene.SourceItemId, scene.FieldId, new[] { scene.TargetItemId }, version));

        await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
    }

    [Fact]
    public async Task AFieldWhoseTargetIsInAnotherWorkspace_IsRefused()
    {
        var scene = await SeedAsync();
        using var owner = _host.CreateProvider(scene.Stack.AccountId, scene.Stack.OwnerId);
        var otherStack = await _host.SeedWorkspaceAsync();
        var otherBoard = await _host.SeedBoardAsync(otherStack, "Other workspace", BoardVisibility.Workspace);

        Func<Task> act = () => OrgTreeTestHost.SendAsync(owner, new CreateBoardFieldCommand(
            scene.SourceBoardId, "Linked", "Relation", $"{{\"targetBoardId\":\"{otherBoard}\"}}"));

        await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
    }

    private sealed record Scene(OrgStack Stack, Guid SourceBoardId, Guid OtherBoardId, Guid SourceItemId, Guid TargetItemId, Guid FieldId);

    private async Task<Scene> SeedAsync()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var source = await _host.SeedBoardAsync(stack, "Source", BoardVisibility.Workspace);
        var target = await _host.SeedBoardAsync(stack, "Target", BoardVisibility.Workspace);
        var other = await _host.SeedBoardAsync(stack, "Other", BoardVisibility.Workspace);
        var sourceItem = await SeedItemAsync(stack, source, "Source item");
        var targetItem = await SeedItemAsync(stack, target, "Target item");
        var field = BoardField.Create(stack.AccountId, stack.WorkspaceId, source, "Linked", FieldType.Relation,
            FieldSettings.Create(JsonValue.Create($"{{\"targetBoardId\":\"{target}\"}}")), FractionalIndex.Create("a5"), stack.OwnerId, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(c =>
        {
            c.BoardFields.Add(field);
            return Task.CompletedTask;
        });
        return new Scene(stack, source, other, sourceItem, targetItem, field.Id);
    }

    private async Task<Guid> SeedItemAsync(OrgStack stack, Guid boardId, string name)
    {
        var group = BoardGroup.Create(stack.AccountId, stack.WorkspaceId, boardId, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), stack.OwnerId, OrgTreeTestHost.BaseTime);
        var item = BoardItem.CreateRoot(stack.AccountId, stack.WorkspaceId, boardId, group.Id, name, FractionalIndex.Create("a0"), stack.OwnerId, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(c =>
        {
            c.BoardGroups.Add(group);
            c.BoardItems.Add(item);
            return Task.CompletedTask;
        });
        return item.Id;
    }

    private async Task<long> VersionAsync(Guid itemId) =>
        (await _host.ReadAsync(c => c.BoardItems.AsNoTracking().SingleAsync(i => i.Id == itemId))).Version;

    private async Task<string?> StoredAsync(Guid itemId, Guid fieldId)
    {
        await using var connection = _db.CreateOwnerConnection();
        await connection.OpenAsync();
        await using var command = new Npgsql.NpgsqlCommand(
            $"SELECT value::text FROM work.board_item_values WHERE item_id = '{itemId}' AND field_id = '{fieldId}'", connection);
        return await command.ExecuteScalarAsync() as string;
    }
}
