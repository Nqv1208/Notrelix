using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.DeleteBoardField;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.RestoreBoardField;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateBoardField;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateFieldOption;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ClearFieldValue;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValue;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
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

    [Fact]
    public async Task ASinglePersonField_TakesOnePersonId_AndRefusesAList()
    {
        var scene = await SeedAsync(FieldType.Person);
        var colleague = await _host.AddMemberAsync(scene.Stack, WorkspaceRole.Member, "Colleague");
        using var owner = Provider(scene.Stack);

        await WriteAsync(owner, scene, colleague.ToString());
        var after = await VersionAsync(scene.ItemId);
        await WriteAsync(owner, scene, colleague.ToString());
        var asList = () => WriteAsync(owner, scene, new[] { colleague.ToString() });

        (await StoredValueAsync(scene.ItemId, scene.FieldId)).Should().Contain(colleague.ToString());
        (await VersionAsync(scene.ItemId)).Should().Be(after, "the same person again is a no-op");
        await asList.Should().ThrowAsync<Exception>("a single field holds one person, not a list");
    }

    [Fact]
    public async Task AMultiplePersonField_TakesAList_HoldsEachPersonOnce_AndTheSameSetIsANoOp()
    {
        var scene = await SeedAsync(FieldType.Person, "{\"multiple\":true}");
        var first = await _host.AddMemberAsync(scene.Stack, WorkspaceRole.Member, "First");
        var second = await _host.AddMemberAsync(scene.Stack, WorkspaceRole.Member, "Second");
        using var owner = Provider(scene.Stack);

        await WriteAsync(owner, scene, new[] { first.ToString(), second.ToString(), first.ToString() });
        var after = await VersionAsync(scene.ItemId);
        var stored = System.Text.Json.JsonSerializer.Deserialize<Guid[]>((await StoredValueAsync(scene.ItemId, scene.FieldId))!)!;
        await WriteAsync(owner, scene, new[] { second.ToString(), first.ToString() });
        var asOne = () => WriteAsync(owner, scene, first.ToString());

        stored.Should().BeEquivalentTo([first, second], "a repeated person is held once");
        (await VersionAsync(scene.ItemId)).Should().Be(after, "the same set in another order is a no-op");
        await asOne.Should().ThrowAsync<Exception>("a multiple field holds a list");

        await WriteAsync(owner, scene, Array.Empty<string>());
        (await StoredValueAsync(scene.ItemId, scene.FieldId)).Should().BeOneOf([null, "null"], "an empty list clears the field");
    }

    [Fact]
    public async Task APersonWhoCannotViewTheBoard_IsRefusedWithTheReasonToShareFirst_AndNothingChanges()
    {
        var scene = await SeedAsync(FieldType.Person, "{\"multiple\":true}", BoardVisibility.Private);
        var cannotSee = await _host.AddMemberAsync(scene.Stack, WorkspaceRole.Member, "Cannot see");
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.ItemId);

        var refused = () => WriteAsync(owner, scene, new[] { cannotSee.ToString() });

        (await refused.Should().ThrowAsync<AppForbidden>()).Which.Code.Should().Be(Notrelix.Application.Features.WorkManagement.Boards.Services.BoardAssigneeGuard.BoardNotSharedCode);
        (await StoredValueAsync(scene.ItemId, scene.FieldId)).Should().BeNull();
        (await VersionAsync(scene.ItemId)).Should().Be(read);
    }

    [Fact]
    public async Task ARenamedOption_KeepsItsId_AndTheItemsStillHoldIt()
    {
        var scene = await SeedAsync(FieldType.Select);
        using var owner = Provider(scene.Stack);
        await WriteAsync(owner, scene, scene.Options[0].ToString());
        var item = await VersionAsync(scene.ItemId);

        await OrgTreeTestHost.SendAsync(owner, new UpdateFieldOptionCommand(
            scene.BoardId, scene.FieldId, scene.Options[0], "Kế toán", "#1666CC", await FieldVersionAsync(scene.FieldId)));

        (await StoredValueAsync(scene.ItemId, scene.FieldId)).Should().Contain(scene.Options[0].ToString());
        (await VersionAsync(scene.ItemId)).Should().Be(item, "renaming an option does not touch the items holding it");
        var option = await _host.ReadAsync(c => c.BoardFields.AsNoTracking().Include(f => f.Options).Where(f => f.Id == scene.FieldId).Select(f => f.Options.Single(o => o.Id == scene.Options[0])).SingleAsync());
        option.Name.Should().Be("Kế toán");
    }

    [Fact]
    public async Task AMultiSelect_HoldsAnOptionOnce_ARepeatedOptionIsRefusedAndTheSetStays()
    {
        var scene = await SeedAsync(FieldType.MultiSelect);
        using var owner = Provider(scene.Stack);
        var (first, second) = (scene.Options[0].ToString(), scene.Options[1].ToString());

        await WriteAsync(owner, scene, new[] { first });
        var after = await VersionAsync(scene.ItemId);
        await WriteAsync(owner, scene, new[] { first });
        var twice = () => WriteAsync(owner, scene, new[] { first, second, first });

        (await VersionAsync(scene.ItemId)).Should().Be(after, "the same set is a no-op");
        await twice.Should().ThrowAsync<Exception>("an option cannot be in the set twice");
        System.Text.Json.JsonSerializer.Deserialize<string[]>((await StoredValueAsync(scene.ItemId, scene.FieldId))!).Should().Equal(first);
        (await VersionAsync(scene.ItemId)).Should().Be(after);
    }

    [Fact]
    public async Task APersonValueOverTheCap_OrWithANonStringElement_IsRefusedBeforeAnyLookup_AndMixedSetsNameTheMissingShare()
    {
        var scene = await SeedAsync(FieldType.Person, "{\"multiple\":true}", BoardVisibility.Private);
        var cannotSee = await _host.AddMemberAsync(scene.Stack, WorkspaceRole.Member, "Cannot see");
        using var owner = Provider(scene.Stack);
        var read = await VersionAsync(scene.ItemId);

        var overCap = () => WriteAsync(owner, scene, Enumerable.Range(0, 51).Select(_ => Guid.NewGuid().ToString()).ToArray());
        var nonString = () => WriteAsync(owner, scene, new object[] { Guid.NewGuid().ToString(), 7 });
        var mixed = () => WriteAsync(owner, scene, new[] { scene.Stack.OwnerId.ToString(), cannotSee.ToString() });

        (await overCap.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>()).Which.Message.Should().Contain("at most 50");
        await nonString.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        (await mixed.Should().ThrowAsync<AppForbidden>()).Which.Code.Should().Be(Notrelix.Application.Features.WorkManagement.Boards.Services.BoardAssigneeGuard.BoardNotSharedCode);
        (await StoredValueAsync(scene.ItemId, scene.FieldId)).Should().BeNull("a refused set stores nothing, not even the person who can see");
        (await VersionAsync(scene.ItemId)).Should().Be(read);
    }

    [Fact]
    public async Task TheFieldRules_Guidance_Required_AndDefault_AreStored_AndReadBack()
    {
        var scene = await SeedAsync(FieldType.Select);
        using var owner = Provider(scene.Stack);

        await OrgTreeTestHost.SendAsync(owner, new UpdateBoardFieldCommand(scene.BoardId, scene.FieldId, null,
            "{\"required\":true,\"guidance\":\"Chọn phòng ban phụ trách\"}", await FieldVersionAsync(scene.FieldId), $"\"{scene.Options[0]}\""));

        var field = await _host.ReadAsync(c => c.BoardFields.AsNoTracking().SingleAsync(f => f.Id == scene.FieldId));
        System.Text.Json.JsonDocument.Parse(field.Settings.Data.Value).RootElement.GetProperty("guidance").GetString().Should().Be("Chọn phòng ban phụ trách");
        field.IsRequired.Should().BeTrue();
        field.DefaultValue!.Data.Value.Should().Contain(scene.Options[0].ToString());

        var badRequired = async () => await OrgTreeTestHost.SendAsync(owner, new UpdateBoardFieldCommand(scene.BoardId, scene.FieldId, null, "{\"required\":\"yes\"}", await FieldVersionAsync(scene.FieldId)));
        await badRequired.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        var foreignDefault = async () => await OrgTreeTestHost.SendAsync(owner, new UpdateBoardFieldCommand(scene.BoardId, scene.FieldId, null, null, await FieldVersionAsync(scene.FieldId), $"\"{Guid.NewGuid()}\""));
        await foreignDefault.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>("a default must be one of the field's options");
    }

    [Fact]
    public async Task ARequiredField_RefusesClearingItsValue_AndTheValueStays()
    {
        var scene = await SeedAsync(FieldType.Text);
        using var owner = Provider(scene.Stack);
        await WriteAsync(owner, scene, "giữ lại");
        await OrgTreeTestHost.SendAsync(owner, new UpdateBoardFieldCommand(scene.BoardId, scene.FieldId, null, "{\"required\":true}", await FieldVersionAsync(scene.FieldId)));
        var version = await VersionAsync(scene.ItemId);

        var byNull = () => WriteAsync(owner, scene, null);
        var byClear = () => OrgTreeTestHost.SendAsync(owner, new ClearFieldValueCommand(scene.ItemId, scene.FieldId, version));

        (await byNull.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>()).Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_FieldValue_Required);
        await byClear.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        (await StoredValueAsync(scene.ItemId, scene.FieldId)).Should().Contain("giữ lại");
        (await VersionAsync(scene.ItemId)).Should().Be(version);
        await WriteAsync(owner, scene, "đổi được");
    }

    [Fact]
    public async Task ANewItem_GetsTheDefault_AndARequiredFieldWithoutOneRefusesTheCreate()
    {
        var scene = await SeedAsync(FieldType.Select);
        using var owner = Provider(scene.Stack);
        var groupId = await _host.ReadAsync(c => c.BoardGroups.AsNoTracking().Where(g => g.BoardId == scene.BoardId).Select(g => g.Id).FirstAsync());
        // The create handler needs the placement stack this host does not wire; the rules it applies are this one call.
        async Task<BoardItem> CreateAsync()
        {
            var item = BoardItem.CreateRoot(scene.Stack.AccountId, scene.Stack.WorkspaceId, scene.BoardId, groupId, "Việc mới", FractionalIndex.Create("a1"), scene.Stack.OwnerId, OrgTreeTestHost.BaseTime);
            await _host.WithSystemContextAsync(async c =>
            {
                await Notrelix.Application.Features.WorkManagement.BoardItems.Services.BoardItemFieldWrites.ApplyCreateRulesAsync(c, item, scene.Stack.OwnerId, OrgTreeTestHost.BaseTime, default);
                c.BoardItems.Add(item);
            });
            return item;
        }

        await OrgTreeTestHost.SendAsync(owner, new UpdateBoardFieldCommand(scene.BoardId, scene.FieldId, null, "{\"required\":true}", await FieldVersionAsync(scene.FieldId)));
        var refused = CreateAsync;
        (await refused.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>()).Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_FieldValue_Required);
        (await _host.ReadAsync(c => c.BoardItems.AsNoTracking().CountAsync(i => i.BoardId == scene.BoardId && i.Name == "Việc mới"))).Should().Be(0);

        await OrgTreeTestHost.SendAsync(owner, new UpdateBoardFieldCommand(scene.BoardId, scene.FieldId, null, null, await FieldVersionAsync(scene.FieldId), $"\"{scene.Options[1]}\""));
        var created = await CreateAsync();

        (await StoredValueAsync(created.Id, scene.FieldId)).Should().Contain(scene.Options[1].ToString(), "the default is applied when the item is created");
        (await StoredValueAsync(scene.ItemId, scene.FieldId)).Should().BeNull("existing items are not rewritten by a default");
    }

    [Fact]
    public async Task AValueNamingAnOptionThatWasRemoved_AnswersPreconditionNamingTheFieldAndTheOption_AndStoresNothing()
    {
        var scene = await SeedAsync(FieldType.Select);
        using var owner = Provider(scene.Stack);
        var removed = Guid.CreateVersion7();
        var read = await VersionAsync(scene.ItemId);

        var stale = () => OrgTreeTestHost.SendAsync(owner, new UpdateBoardItemFieldValueCommand(scene.ItemId, scene.FieldId, removed.ToString(), read));

        var error = (await stale.Should().ThrowAsync<PreconditionFailed>()).Which;
        error.ErrorCode.Should().Be("work-management.field.option-removed");
        error.Extensions["fieldName"].Should().Be("Note");
        error.Extensions["optionId"].Should().Be(removed);
        (await StoredValueAsync(scene.ItemId, scene.FieldId)).Should().BeNull();
        (await VersionAsync(scene.ItemId)).Should().Be(read);
    }

    private record Scene(OrgStack Stack, Guid BoardId, Guid ItemId, Guid FieldId, IReadOnlyList<Guid> Options);

    private async Task<Scene> SeedAsync(FieldType type, string? settings = null, BoardVisibility visibility = BoardVisibility.Workspace)
    {
        var stack = await _host.SeedWorkspaceAsync();
        var boardId = await _host.SeedBoardAsync(stack, "Fields", visibility);
        var group = BoardGroup.Create(stack.AccountId, stack.WorkspaceId, boardId, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), stack.OwnerId, OrgTreeTestHost.BaseTime);
        var item = BoardItem.CreateRoot(stack.AccountId, stack.WorkspaceId, boardId, group.Id, "Task", FractionalIndex.Create("a0"), stack.OwnerId, OrgTreeTestHost.BaseTime);
        var field = BoardField.Create(stack.AccountId, stack.WorkspaceId, boardId, "Note", type, settings is null ? FieldSettings.Empty() : FieldSettings.Create(JsonValue.Create(settings)), FractionalIndex.Create("a5"), stack.OwnerId, OrgTreeTestHost.BaseTime);
        var options = type is FieldType.Select or FieldType.MultiSelect
            ? new[] { "Accounting", "Warehouse" }.Select((name, index) =>
                field.AddOption(name, Color.Create("#1666CC"), FractionalIndex.Create($"a{index}"), stack.OwnerId, OrgTreeTestHost.BaseTime).Id).ToList()
            : [];
        await _host.WithSystemContextAsync(c =>
        {
            c.BoardGroups.Add(group);
            c.BoardItems.Add(item);
            c.BoardFields.Add(field);
            return Task.CompletedTask;
        });
        return new Scene(stack, boardId, item.Id, field.Id, options);
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
