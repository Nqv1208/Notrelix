using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.DeleteBoardField;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.RestoreBoardField;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateBoardField;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValue;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;
using Conflict = Notrelix.Application.Common.Exceptions.ConflictException;
using PreconditionFailed = Notrelix.Application.Common.Exceptions.PreconditionFailedException;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// Custom field values and the field's life through the real pipeline on PostgreSQL (NRX-003, NRX-004, NRX-007):
/// a value of each type is written once and the same value again changes nothing; a refused value, a stale
/// version and a value written to a field deleted since the client loaded the board store nothing and bump no
/// version; a deleted field names itself and who deleted it, an unknown one and an outsider get the identical
/// NotFound (UC-WM-D01, D02, D03, D12); a field is deleted with its version, restored inside the 30-day window
/// with its values, and refused after it (UC-WM-C04).
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class FieldValuesAndLifecyclePipelineIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public FieldValuesAndLifecyclePipelineIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData(FieldType.Text, "Cần xuất bản Excel", "Another note")]
    [InlineData(FieldType.LongText, "Dòng một\nDòng hai", "Dòng một")]
    [InlineData(FieldType.Number, 13, 14)]
    [InlineData(FieldType.Checkbox, true, false)]
    [InlineData(FieldType.Link, "https://erp.example.vn/t/145", "https://erp.example.vn/t/146")]
    public async Task AValue_IsWrittenOnce_TheSameValueAgainChangesNothing_AndClearingTwiceChangesNothingTheSecondTime(FieldType type, object first, object second)
    {
        var scene = await SeedAsync(type);
        using var owner = Provider(scene.Stack);

        await WriteAsync(owner, scene, first);
        var afterFirst = await VersionAsync(scene.ItemId);
        await WriteAsync(owner, scene, first);
        (await VersionAsync(scene.ItemId)).Should().Be(afterFirst, "the same value is a semantic no-op");

        await WriteAsync(owner, scene, second);
        var afterSecond = await VersionAsync(scene.ItemId);
        afterSecond.Should().BeGreaterThan(afterFirst);

        await WriteAsync(owner, scene, null);
        var cleared = await VersionAsync(scene.ItemId);
        (await StoredValueAsync(scene.ItemId, scene.FieldId)).Should().BeOneOf([null, "null"], "a cleared value holds nothing");
        await WriteAsync(owner, scene, null);
        (await VersionAsync(scene.ItemId)).Should().Be(cleared, "clearing an empty value changes nothing");
    }

    [Theory]
    [InlineData(FieldType.Number, "13a")]
    [InlineData(FieldType.Link, "javascript:alert(1)")]
    [InlineData(FieldType.Checkbox, "yes")]
    [InlineData(FieldType.Date, "32/13/2026")]
    public async Task AnInvalidValue_IsRefused_AndNothingIsStoredOrVersioned(FieldType type, object invalid)
    {
        var scene = await SeedAsync(type);
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.ItemId);

        var act = () => WriteAsync(owner, scene, invalid);

        await act.Should().ThrowAsync<Exception>();
        (await StoredValueAsync(scene.ItemId, scene.FieldId)).Should().BeNull();
        (await VersionAsync(scene.ItemId)).Should().Be(read);
    }

    [Fact]
    public async Task AWriteWithAStaleVersion_IsRefused_AndTheStoredValueStays()
    {
        var scene = await SeedAsync(FieldType.Text);
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.ItemId);
        await WriteAsync(owner, scene, "theirs");
        var current = await VersionAsync(scene.ItemId);

        var stale = () => OrgTreeTestHost.SendAsync(owner, new UpdateBoardItemFieldValueCommand(scene.ItemId, scene.FieldId, "mine", read));

        await stale.Should().ThrowAsync<PreconditionFailed>();
        (await StoredValueAsync(scene.ItemId, scene.FieldId)).Should().Contain("theirs");
        (await VersionAsync(scene.ItemId)).Should().Be(current);
    }

    [Fact]
    public async Task AValueWrittenToAFieldDeletedSinceTheClientLoadedTheBoard_AnswersPreconditionNamingTheField_AndStoresNothing()
    {
        var scene = await SeedAsync(FieldType.Text);
        using var owner = Provider(scene.Stack);
        await OrgTreeTestHost.SendAsync(owner, new DeleteBoardFieldCommand(scene.BoardId, scene.FieldId, await FieldVersionAsync(scene.FieldId)));
        var read = await VersionAsync(scene.ItemId);

        var stale = () => OrgTreeTestHost.SendAsync(owner, new UpdateBoardItemFieldValueCommand(scene.ItemId, scene.FieldId, "late", read));

        var error = (await stale.Should().ThrowAsync<PreconditionFailed>()).Which;
        error.ErrorCode.Should().Be("work-management.field.deleted");
        error.Extensions["fieldName"].Should().Be("Note");
        (await StoredValueAsync(scene.ItemId, scene.FieldId)).Should().BeNull();
        (await VersionAsync(scene.ItemId)).Should().Be(read);

        var unknown = () => OrgTreeTestHost.SendAsync(owner, new UpdateBoardItemFieldValueCommand(scene.ItemId, Guid.CreateVersion7(), "x", read));
        await unknown.Should().ThrowAsync<AppNotFound>("a field the board never had stays the plain NotFound");
    }

    [Fact]
    public async Task APersonOutsideTheWorkspace_GetsTheNotFoundAMissingItemGets_AndNothingIsStored()
    {
        var scene = await SeedAsync(FieldType.Text);
        var version = await VersionAsync(scene.ItemId);
        var outsider = await _host.AddOutsiderAsync(scene.Stack);
        var otherWorkspace = await _host.SeedWorkspaceAsync();

        foreach (var (user, account) in new[] { (outsider, scene.Stack.AccountId), (otherWorkspace.OwnerId, otherWorkspace.AccountId) })
        {
            using var provider = _host.CreateProvider(account, user);
            var act = () => OrgTreeTestHost.SendAsync(provider, new UpdateBoardItemFieldValueCommand(scene.ItemId, scene.FieldId, "x", version));
            await act.Should().ThrowAsync<AppNotFound>();
        }

        (await StoredValueAsync(scene.ItemId, scene.FieldId)).Should().BeNull();
    }

    [Fact]
    public async Task ADeletedField_IsRestoredWithItsValues_AndRestoringAnActiveFieldIsANoOp()
    {
        var scene = await SeedAsync(FieldType.Text);
        using var owner = Provider(scene.Stack);
        await WriteAsync(owner, scene, "kept");
        await OrgTreeTestHost.SendAsync(owner, new DeleteBoardFieldCommand(scene.BoardId, scene.FieldId, await FieldVersionAsync(scene.FieldId)));
        (await FieldDeletedAtAsync(scene.FieldId)).Should().NotBeNull();

        await OrgTreeTestHost.SendAsync(owner, new RestoreBoardFieldCommand(scene.BoardId, scene.FieldId));
        (await FieldDeletedAtAsync(scene.FieldId)).Should().BeNull();
        (await StoredValueAsync(scene.ItemId, scene.FieldId)).Should().Contain("kept", "a restored field brings its values back");

        var version = await FieldVersionAsync(scene.FieldId);
        await OrgTreeTestHost.SendAsync(owner, new RestoreBoardFieldCommand(scene.BoardId, scene.FieldId));
        (await FieldVersionAsync(scene.FieldId)).Should().Be(version, "restoring an active field changes nothing");
    }

    [Fact]
    public async Task ADeletedField_CannotBeRestoredAfter30Days_AndTheValuesStayStored()
    {
        var scene = await SeedAsync(FieldType.Text);
        using var owner = Provider(scene.Stack);
        await WriteAsync(owner, scene, "kept");
        await OrgTreeTestHost.SendAsync(owner, new DeleteBoardFieldCommand(scene.BoardId, scene.FieldId, await FieldVersionAsync(scene.FieldId)));
        _host.Now = OrgTreeTestHost.BaseTime.AddDays(31);

        var late = () => OrgTreeTestHost.SendAsync(owner, new RestoreBoardFieldCommand(scene.BoardId, scene.FieldId));

        var error = (await late.Should().ThrowAsync<Conflict>()).Which;
        error.ErrorCode.Should().Be("work-management.field.restore-window-closed");
        (await FieldDeletedAtAsync(scene.FieldId)).Should().NotBeNull("the field stays deleted; nothing is purged in this release");
        (await StoredValueAsync(scene.ItemId, scene.FieldId)).Should().Contain("kept");
    }

    [Fact]
    public async Task ADeleteWithAStaleFieldVersion_IsRefused_AndTheFieldStaysLive()
    {
        var scene = await SeedAsync(FieldType.Text);
        using var owner = Provider(scene.Stack);
        var read = await FieldVersionAsync(scene.FieldId);
        await OrgTreeTestHost.SendAsync(owner, new UpdateBoardFieldCommand(scene.BoardId, scene.FieldId, "Renamed", null, read));

        var stale = () => OrgTreeTestHost.SendAsync(owner, new DeleteBoardFieldCommand(scene.BoardId, scene.FieldId, read));

        await stale.Should().ThrowAsync<PreconditionFailed>();
        (await FieldDeletedAtAsync(scene.FieldId)).Should().BeNull();
    }

    private record Scene(OrgStack Stack, Guid BoardId, Guid ItemId, Guid FieldId);

    private async Task<Scene> SeedAsync(FieldType type)
    {
        var stack = await _host.SeedWorkspaceAsync();
        var boardId = await _host.SeedBoardAsync(stack, "Fields", BoardVisibility.Workspace);
        var group = BoardGroup.Create(stack.AccountId, stack.WorkspaceId, boardId, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), stack.OwnerId, OrgTreeTestHost.BaseTime);
        var item = BoardItem.CreateRoot(stack.AccountId, stack.WorkspaceId, boardId, group.Id, "Task", FractionalIndex.Create("a0"), stack.OwnerId, OrgTreeTestHost.BaseTime);
        var field = BoardField.Create(stack.AccountId, stack.WorkspaceId, boardId, "Note", type, FieldSettings.Empty(), FractionalIndex.Create("a5"), stack.OwnerId, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(c =>
        {
            c.BoardGroups.Add(group);
            c.BoardItems.Add(item);
            c.BoardFields.Add(field);
            return Task.CompletedTask;
        });
        return new Scene(stack, boardId, item.Id, field.Id);
    }

    private ServiceProvider Provider(OrgStack stack) => _host.CreateProvider(stack.AccountId, stack.OwnerId);

    private async Task WriteAsync(ServiceProvider provider, Scene scene, object? value) =>
        await OrgTreeTestHost.SendAsync(provider, new UpdateBoardItemFieldValueCommand(scene.ItemId, scene.FieldId, value, await VersionAsync(scene.ItemId)));

    private async Task<long> VersionAsync(Guid itemId) =>
        (await _host.ReadAsync(c => c.BoardItems.AsNoTracking().SingleAsync(i => i.Id == itemId))).Version;

    private Task<long> FieldVersionAsync(Guid fieldId) =>
        _host.ReadAsync(c => c.BoardFields.IgnoreQueryFilters().AsNoTracking().Where(f => f.Id == fieldId).Select(f => f.Version).SingleAsync());

    private Task<DateTimeOffset?> FieldDeletedAtAsync(Guid fieldId) =>
        _host.ReadAsync(c => c.BoardFields.IgnoreQueryFilters().AsNoTracking().Where(f => f.Id == fieldId).Select(f => f.DeletedAt).SingleAsync());

    private async Task<string?> StoredValueAsync(Guid itemId, Guid fieldId)
    {
        await using var connection = _db.CreateOwnerConnection();
        await connection.OpenAsync();
        await using var command = new Npgsql.NpgsqlCommand(
            $"SELECT value::text FROM work.board_item_values WHERE item_id = '{itemId}' AND field_id = '{fieldId}'", connection);
        return await command.ExecuteScalarAsync() as string;
    }
}
