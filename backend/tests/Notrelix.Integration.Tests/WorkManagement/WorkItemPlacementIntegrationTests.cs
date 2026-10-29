using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.AddFieldOption;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.RestoreBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.CreateBoardField;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.MoveFieldOption;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.RemoveFieldOption;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateFieldOption;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.MoveBoardField;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateBoardField;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.CreateBoardGroup;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.DuplicateBoardGroup;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.MoveBoardGroup;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.CreateBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.DuplicateBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnlinkPageFromBoardItem;
using Notrelix.Domain.WorkManagement.Checklists;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UpdateBoardItemFieldValues;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Ordering;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.WorkManagement;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// ITEM-APP-001 / ITEM-APP-002 / ORDER-DUP-001 (WM-V3-APP-ITEM-001/002, WM-V3-APP-GROUP-001). Item create and move run through the real
/// handlers, the PostgreSQL ordering lock and the placement resolver, each inside
/// a transaction as the data session runs them. Created items get distinct
/// ordered keys (the baseline wrote "a0" for every item), neighbor intents are
/// honoured, stale intents change nothing, and a move to the current place is a
/// semantic no-op.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkItemPlacementIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private DatabaseReset _reset = null!;
    private Guid _accountId;
    private Guid _workspaceId;
    private Guid _userId;
    private Guid _boardId;
    private Guid _todoId;
    private Guid _doneId;

    public WorkItemPlacementIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        _reset = new DatabaseReset(_db.OwnerConnectionString);
        await _reset.ResetAsync();

        _accountId = Guid.NewGuid();
        _userId = Guid.NewGuid();
        var workspace = Workspace.Create(_accountId, _userId, "Placement WS", $"place-{Guid.NewGuid():N}", Now);
        var board = Board.Create(_accountId, workspace.Id, _userId, "Placement Board", null, Now);
        var todo = BoardGroup.Create(_accountId, workspace.Id, board.Id, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), _userId, Now);
        var done = BoardGroup.Create(_accountId, workspace.Id, board.Id, "Done", Color.Create("#808080"), FractionalIndex.Create("a1"), _userId, Now);
        (_workspaceId, _boardId, _todoId, _doneId) = (workspace.Id, board.Id, todo.Id, done.Id);

        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Workspaces.Add(workspace);
        seed.Boards.Add(board);
        seed.BoardGroups.AddRange(todo, done);
        await seed.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Create_AppendsDistinctOrderedKeys()
    {
        var first = await CreateAsync("first");
        var second = await CreateAsync("second");
        var third = await CreateAsync("third");

        (await OrderInGroupAsync(_todoId)).Should().Equal(first, second, third);
        (await PositionsInGroupAsync(_todoId)).Should().OnlyHaveUniqueItems("every created item gets its own key");
    }

    [Fact]
    public async Task Create_BetweenAdjacentNeighbors_PlacesBetweenThem()
    {
        var first = await CreateAsync("first");
        var last = await CreateAsync("last");

        var middle = await CreateAsync("middle", previous: first, next: last);

        (await OrderInGroupAsync(_todoId)).Should().Equal(first, middle, last);
    }

    [Fact]
    public async Task Move_IntoAnotherGroup_HonoursTheNeighborIntent()
    {
        var mover = await CreateAsync("mover");
        var a = await CreateAsync("a", group: _doneId);
        var b = await CreateAsync("b", group: _doneId);

        await MoveAsync(mover, _doneId, previous: a, next: b);

        (await OrderInGroupAsync(_doneId)).Should().Equal(a, mover, b);
        (await OrderInGroupAsync(_todoId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Move_WithStaleNeighbors_IsRejected_AndChangesNothing()
    {
        var mover = await CreateAsync("mover");
        var a = await CreateAsync("a", group: _doneId);
        var b = await CreateAsync("b", group: _doneId);
        var c = await CreateAsync("c", group: _doneId);
        var versionBefore = await VersionAsync(mover);

        var act = () => MoveAsync(mover, _doneId, previous: a, next: c);

        (await act.Should().ThrowAsync<ConflictException>()).Which.ErrorCode.Should().Be(WorkOrderingErrorCodes.StalePlacement);
        (await OrderInGroupAsync(_doneId)).Should().Equal(a, b, c);
        (await VersionAsync(mover)).Should().Be(versionBefore);
    }

    [Fact]
    public async Task Move_ToItsCurrentPlace_IsASemanticNoOp()
    {
        var first = await CreateAsync("first");
        var middle = await CreateAsync("middle");
        var last = await CreateAsync("last");
        var versionBefore = await VersionAsync(middle);

        await MoveAsync(middle, _todoId, previous: first, next: last);

        (await VersionAsync(middle)).Should().Be(versionBefore, "re-placing an item where it already is changes nothing");
        (await OrderInGroupAsync(_todoId)).Should().Equal(first, middle, last);
    }

    [Fact]
    public async Task DuplicateItem_IsPlacedDirectlyAfterItsSource()
    {
        var first = await CreateAsync("first");
        var second = await CreateAsync("second");

        var copy = await InTransactionAsync(async context =>
            (await new DuplicateBoardItemCommandHandler(context, Request(), Clock(), Resolver(context), Keys(context))
                .Handle(new DuplicateBoardItemCommand(first), CancellationToken.None)).Data);

        (await OrderInGroupAsync(_todoId)).Should().Equal(first, copy, second);
    }

    [Fact]
    public async Task DuplicateItem_WithOptions_CopiesChecklistsSubItemsAndAssignees_AndKeepsTheSource()
    {
        var source = await CreateAsync("source");
        var assignee = Guid.NewGuid();
        await using (var seed = _db.CreateOwnerContext(SystemTenant()))
        {
            seed.Checklists.Add(Checklist.Create(_accountId, _workspaceId, source, "Kiểm tra", FractionalIndex.Create("a0"), _userId, Now));
            seed.BoardItemMembers.Add(BoardItemMember.Create(_accountId, _workspaceId, _boardId, source, assignee, _userId, Now));
            await seed.SaveChangesAsync();
        }
        await InTransactionAsync(async context =>
        {
            var parent = await context.BoardItems.SingleAsync(i => i.Id == source);
            var key = (await Keys(context).AllocateAsync(context, _boardId, 1, CancellationToken.None))[0];
            context.BoardItems.Add(BoardItem.CreateSubItem(parent, "con", FractionalIndex.Create("a0"), _userId, Now, key.Key, key.Sequence));
            return true;
        });

        var copy = await InTransactionAsync(async context =>
            (await new DuplicateBoardItemCommandHandler(context, Request(), Clock(), Resolver(context), Keys(context))
                .Handle(new DuplicateBoardItemCommand(source, "Bản sao", new DuplicateBoardItemOptions(IncludeChecklists: true, IncludeSubItems: true, IncludeAssignees: true)), CancellationToken.None)).Data);

        await using var read = _db.CreateOwnerContext(SystemTenant());
        (await read.BoardItems.AnyAsync(i => i.ParentItemId == copy && i.Name == "con")).Should().BeTrue();
        (await read.Checklists.CountAsync(c => c.ItemId == copy)).Should().Be(1);
        (await read.BoardItemMembers.CountAsync(m => m.ItemId == copy && m.UserId == assignee)).Should().Be(1);
        (await read.Checklists.CountAsync(c => c.ItemId == source)).Should().Be(1);
        (await read.BoardItems.CountAsync(i => i.ParentItemId == source)).Should().Be(1);
        (await read.BoardItems.Select(i => i.ItemKey).Distinct().CountAsync()).Should().Be(await read.BoardItems.CountAsync());
    }

    [Fact]
    public async Task UnlinkPage_RemovesOnlyThatPageLink_NeverItemToItemLinks()
    {
        var a = await CreateAsync("a");
        var b = await CreateAsync("b");
        var pageKind = ResourceKind.Create("documents.page");
        var kept = Guid.NewGuid();
        var removed = Guid.NewGuid();
        await using (var seed = _db.CreateOwnerContext(SystemTenant()))
        {
            seed.BoardItemLinks.AddRange(
                BoardItemLink.Create(_accountId, _workspaceId, _boardId, a, ResourceRef.Create(ResourceKind.Create("work-management.board-item"), b, _workspaceId), BoardItemLinkType.Reference, _userId, Now),
                BoardItemLink.Create(_accountId, _workspaceId, _boardId, a, ResourceRef.Create(pageKind, kept, _workspaceId), BoardItemLinkType.Reference, _userId, Now),
                BoardItemLink.Create(_accountId, _workspaceId, _boardId, a, ResourceRef.Create(pageKind, removed, _workspaceId), BoardItemLinkType.Reference, _userId, Now));
            await seed.SaveChangesAsync();
        }

        await InTransactionAsync(async context =>
        {
            await new UnlinkPageFromBoardItemCommandHandler(context, CurrentUser())
                .Handle(new UnlinkPageFromBoardItemCommand(a, removed), CancellationToken.None);
            return true;
        });

        await using var read = _db.CreateOwnerContext(SystemTenant());
        var left = await read.BoardItemLinks.Where(l => l.SourceItemId == a).ToListAsync();
        left.Should().HaveCount(2);
        left.Select(l => l.Target.ResourceId).Should().BeEquivalentTo([b, kept]);
    }

    [Fact]
    public async Task Groups_AreCreatedMovedAndDuplicatedRelativeToTheirNeighbors()
    {
        var third = await CreateGroupAsync("third");

        await InTransactionAsync(async context =>
            await new MoveBoardGroupCommandHandler(context, Request(), Clock(), Resolver(context))
                .Handle(new MoveBoardGroupCommand(third, 1, _todoId, _doneId), CancellationToken.None));
        (await GroupOrderAsync()).Should().Equal(_todoId, third, _doneId);

        var a = await CreateAsync("a");
        var b = await CreateAsync("b");
        var copy = await InTransactionAsync(async context =>
            (await new DuplicateBoardGroupCommandHandler(context, Request(), Clock(), Resolver(context), Keys(context))
                .Handle(new DuplicateBoardGroupCommand(_todoId), CancellationToken.None)).Data);

        (await GroupOrderAsync()).Should().Equal(_todoId, copy, third, _doneId);
        var clonedTitles = await TitlesInGroupAsync(copy);
        clonedTitles.Should().Equal("a", "b");
        (await OrderInGroupAsync(_todoId)).Should().Equal(a, b);
    }

    [Fact]
    public async Task BulkFieldValues_WithAnUnsupportedField_ChangeNothing()
    {
        var item = await CreateAsync("original");

        var result = await InTransactionAsync(async context =>
            await new UpdateBoardItemFieldValuesCommandHandler(context, Request(), Clock(), Mock.Of<IResourceLocator>())
                .Handle(new UpdateBoardItemFieldValuesCommand(item, new Dictionary<Guid, object?>
                {
                    [_boardId] = "renamed",
                    [Guid.NewGuid()] = "unknown field",
                }, 1), CancellationToken.None));

        result.Succeeded.Should().BeFalse();
        (await TitlesInGroupAsync(_todoId)).Should().Equal(["original"], "a rejected bulk update must not apply any value (WMREQ025)");
    }

    [Fact]
    public async Task Fields_AreCreatedAndMovedRelativeToTheirNeighbors()
    {
        var a = await CreateFieldAsync("A");
        var b = await CreateFieldAsync("B");
        var c = await CreateFieldAsync("C", previous: a, next: b);
        (await FieldOrderAsync()).Should().Equal(a, c, b);

        await InTransactionAsync(async context =>
            await new MoveBoardFieldCommandHandler(context, Request(), Clock(), Resolver(context))
                .Handle(new MoveBoardFieldCommand(_boardId, a, 1, b, null), CancellationToken.None));

        (await FieldOrderAsync()).Should().Equal(c, b, a);
    }

    [Fact]
    public async Task FieldPatch_RenameOnly_RenamesAndKeepsSettings()
    {
        var field = await CreateFieldAsync("Status", type: "Status");
        var settingsBefore = await FieldSettingsAsync(field);

        await InTransactionAsync(async context =>
        {
            var currentUser = new Mock<ICurrentUser>();
            currentUser.Setup(u => u.UserId).Returns(_userId);
            return await new UpdateBoardFieldCommandHandler(context, currentUser.Object, Clock())
                .Handle(new UpdateBoardFieldCommand(_boardId, field, "Stage", null, 1), CancellationToken.None);
        });

        await using var verify = _db.CreateOwnerContext(SystemTenant());
        var loaded = await verify.BoardFields.SingleAsync(f => f.Id == field);
        loaded.Name.Should().Be("Stage");
        (await FieldSettingsAsync(field)).Should().Be(settingsBefore);
    }

    [Fact]
    public async Task Options_AreAddedAndMovedRelativeToTheirNeighbors_UnderTheFieldLock()
    {
        var field = await CreateFieldAsync("Priority", type: "Select");
        var a = await AddOptionAsync(field, "A");
        var b = await AddOptionAsync(field, "B");
        var c = await AddOptionAsync(field, "C", previous: a, next: b);
        (await OptionOrderAsync(field)).Should().Equal(a, c, b);

        await MoveOptionAsync(field, a, previous: b, next: null);
        (await OptionOrderAsync(field)).Should().Equal(c, b, a);

        var versionBefore = await FieldVersionAsync(field);
        await MoveOptionAsync(field, a, previous: b, next: null);
        (await FieldVersionAsync(field)).Should().Be(versionBefore, "moving an option to its current place is a semantic no-op");
    }

    [Fact]
    public async Task Options_AreUpdatedRemovedAndDeduplicated_FromAFreshRequest()
    {
        var field = await CreateFieldAsync("Priority", type: "Select");
        var a = await AddOptionAsync(field, "A");
        var b = await AddOptionAsync(field, "B");

        await InTransactionAsync(async context =>
            await new UpdateFieldOptionCommandHandler(context, CurrentUser(), Clock())
                .Handle(new UpdateFieldOptionCommand(_boardId, field, a, "Alpha", "#112233", 1), CancellationToken.None));
        await InTransactionAsync(async context =>
            await new RemoveFieldOptionCommandHandler(context, CurrentUser(), Clock())
                .Handle(new RemoveFieldOptionCommand(_boardId, field, b, 1), CancellationToken.None));
        var duplicate = () => AddOptionAsync(field, "alpha");

        await duplicate.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>(
            "the option set must be loaded with the field, so the duplicate-name rule sees persisted options");
        await using var verify = _db.CreateOwnerContext(SystemTenant());
        var options = await verify.FieldOptions.Where(o => o.FieldId == field).ToListAsync();
        options.Should().ContainSingle().Which.Name.Should().Be("Alpha");
    }

    [Fact]
    public async Task NormalDrag_RewritesOnlyTheMovedItemsKey()
    {
        var a = await CreateAsync("A");
        var b = await CreateAsync("B");
        var c = await CreateAsync("C");
        var before = await KeysAsync();

        await MoveAsync(a, _todoId, previous: b, next: c);

        var after = await KeysAsync();
        after.Where(kv => kv.Key != a).Should().BeEquivalentTo(before.Where(kv => kv.Key != a),
            "a normal drag is a single-entity mutation (WM-V3-QUAL-002); siblings keep their keys");
        after[a].Should().NotBe(before[a]);
        (await OrderInGroupAsync(_todoId)).Should().Equal(b, a, c);
    }

    private async Task<Dictionary<Guid, string>> KeysAsync()
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        return await context.BoardItems.Where(i => i.GroupId == _todoId).ToDictionaryAsync(i => i.Id, i => i.Position.Value);
    }

    [Fact]
    public async Task RestoreItem_WhoseKeyWasReissued_LandsRightAfterItsHolder_WithoutAConflict()
    {
        var a = await CreateAsync("A");
        var b = await CreateAsync("B");
        await InTransactionAsync(async context =>
        {
            var item = await context.BoardItems.SingleAsync(i => i.Id == b);
            item.Delete(_userId, Now);
            return true;
        });
        var c = await CreateAsync("C");
        await using (var probe = _db.CreateOwnerContext(SystemTenant()))
        {
            var keys = await probe.BoardItems.IgnoreQueryFilters().Where(i => i.Id == b || i.Id == c).ToDictionaryAsync(i => i.Id, i => i.Position.Value);
            keys[c].Should().Be(keys[b], "the scenario requires the deleted item's key to be reissued");
        }

        await InTransactionAsync(async context =>
            await new RestoreBoardItemCommandHandler(context, Request(), Clock(), Resolver(context))
                .Handle(new RestoreBoardItemCommand(b), CancellationToken.None));

        (await OrderInGroupAsync(_todoId)).Should().Equal(a, c, b);
    }

    private async Task<Guid> AddOptionAsync(Guid fieldId, string name, Guid? previous = null, Guid? next = null) =>
        await InTransactionAsync(async context =>
            (await new AddFieldOptionCommandHandler(context, CurrentUser(), Clock(), Resolver(context))
                .Handle(new AddFieldOptionCommand(_boardId, fieldId, name, "#808080", 1, previous, next), CancellationToken.None)).Data);

    private Task MoveOptionAsync(Guid fieldId, Guid optionId, Guid? previous, Guid? next) =>
        InTransactionAsync(async context =>
            await new MoveFieldOptionCommandHandler(context, CurrentUser(), Clock(), Resolver(context))
                .Handle(new MoveFieldOptionCommand(_boardId, fieldId, optionId, 1, previous, next), CancellationToken.None));

    private async Task<List<Guid>> OptionOrderAsync(Guid fieldId)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        var rows = await context.FieldOptions.Where(o => o.FieldId == fieldId).Select(o => new { o.Id, o.Position }).ToListAsync();
        return rows.OrderBy(r => r.Position).ThenBy(r => r.Id).Select(r => r.Id).ToList();
    }

    private async Task<long> FieldVersionAsync(Guid fieldId)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        return await context.BoardFields.Where(f => f.Id == fieldId).Select(f => f.Version).SingleAsync();
    }

    private ICurrentUser CurrentUser()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.UserId).Returns(_userId);
        return currentUser.Object;
    }

    private async Task<Guid> CreateFieldAsync(string name, string type = "Text", Guid? previous = null, Guid? next = null) =>
        await InTransactionAsync(async context =>
            (await new CreateBoardFieldCommandHandler(context, Request(), Clock(), Resolver(context))
                .Handle(new CreateBoardFieldCommand(_boardId, name, type, null, previous, next), CancellationToken.None)).Data);

    private async Task<List<Guid>> FieldOrderAsync()
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        var rows = await context.BoardFields.Where(f => f.BoardId == _boardId).Select(f => new { f.Id, f.Position }).ToListAsync();
        return rows.OrderBy(r => r.Position).ThenBy(r => r.Id).Select(r => r.Id).ToList();
    }

    private async Task<string> FieldSettingsAsync(Guid fieldId)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        var field = await context.BoardFields.SingleAsync(f => f.Id == fieldId);
        return field.Settings.Data.Value;
    }

    private async Task<Guid> CreateGroupAsync(string title, Guid? previous = null, Guid? next = null) =>
        await InTransactionAsync(async context =>
            (await new CreateBoardGroupCommandHandler(context, Request(), Clock(), Resolver(context))
                .Handle(new CreateBoardGroupCommand(_boardId, title, null, previous, next), CancellationToken.None)).Data);

    private async Task<List<Guid>> GroupOrderAsync()
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        var rows = await context.BoardGroups.Where(g => g.BoardId == _boardId).Select(g => new { g.Id, g.Position }).ToListAsync();
        return rows.OrderBy(r => r.Position).ThenBy(r => r.Id).Select(r => r.Id).ToList();
    }

    private async Task<List<string>> TitlesInGroupAsync(Guid groupId)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        var rows = await context.BoardItems.Where(i => i.GroupId == groupId).Select(i => new { i.Name, i.Position }).ToListAsync();
        return rows.OrderBy(r => r.Position).Select(r => r.Name).ToList();
    }

    private async Task<Guid> CreateAsync(string title, Guid? group = null, Guid? previous = null, Guid? next = null)
    {
        return await InTransactionAsync(async context =>
        {
            var handler = new CreateBoardItemCommandHandler(context, Request(), Clock(), Resolver(context), Keys(context));
            var created = await handler.Handle(new CreateBoardItemCommand(_boardId, group ?? _todoId, title, previous, next), CancellationToken.None);
            return created.Id;
        });
    }

    private Task MoveAsync(Guid itemId, Guid group, Guid? previous, Guid? next) =>
        InTransactionAsync(async context =>
        {
            var currentUser = new Mock<ICurrentUser>();
            currentUser.Setup(u => u.UserId).Returns(_userId);
            var handler = new MoveBoardItemCommandHandler(new MoveBoardItemUseCase(context, Clock(), Resolver(context)), currentUser.Object);
            await handler.Handle(new MoveBoardItemCommand(itemId, group, 1, previous, next), CancellationToken.None);
            return true;
        });

    private async Task<T> InTransactionAsync<T>(Func<ApplicationDbContext, Task<T>> work)
    {
        await using var context = _db.CreateOwnerContext(Tenant());
        await using var transaction = await context.Database.BeginTransactionAsync();
        var result = await work(context);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return result;
    }

    private async Task<List<Guid>> OrderInGroupAsync(Guid groupId)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        var rows = await context.BoardItems.Where(i => i.GroupId == groupId).Select(i => new { i.Id, i.Position }).ToListAsync();
        return rows.OrderBy(r => r.Position).Select(r => r.Id).ToList();
    }

    private async Task<List<string>> PositionsInGroupAsync(Guid groupId)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        var rows = await context.BoardItems.Where(i => i.GroupId == groupId).Select(i => i.Position).ToListAsync();
        return rows.Select(p => p.Value).ToList();
    }

    private async Task<long> VersionAsync(Guid itemId)
    {
        await using var context = _db.CreateOwnerContext(SystemTenant());
        return await context.BoardItems.Where(i => i.Id == itemId).Select(i => i.Version).SingleAsync();
    }

    private static WorkPlacementResolver Resolver(ApplicationDbContext context) => new(new PostgresWorkOrderingScopeLock(context));

    private static IBoardItemKeyAllocator Keys(ApplicationDbContext context) => new PostgresBoardItemKeyAllocator(context);

    private static IDateTimeProvider Clock() => FakeDateTimeProvider.WithFixedTime(Now);

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
