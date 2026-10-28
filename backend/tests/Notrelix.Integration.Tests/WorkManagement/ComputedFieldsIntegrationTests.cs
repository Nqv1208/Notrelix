using Notrelix.Application.Features.Governance.Public.Visibility;
using Notrelix.Application.Features.WorkManagement.BoardFields;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.GetFullBoard;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Application.Features.WorkManagement.Ports.Collaboration;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// Formula and Rollup v1 on PostgreSQL through the real board read: <c>[Effort]*2</c> shows the number and
/// follows a source edit, a sub-item sum ignores archived sub-items, an error is an explicit state, a cycle
/// is rejected on save with its rule code, and another workspace's board is neither read nor leaked.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class ComputedFieldsIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 3, 40, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private Guid _accountId;
    private Guid _workspaceId;
    private Guid _userId;
    private Board _board = null!;
    private BoardGroup _group = null!;
    private BoardField _effort = null!;
    private BoardField _double = null!;
    private BoardField _ratio = null!;
    private BoardField _sum = null!;
    private BoardField _count = null!;
    private BoardItem _parent = null!;
    private BoardItem _childA = null!;
    private BoardItem _childB = null!;
    private BoardItem _archivedChild = null!;
    private Guid _otherWorkspaceId;
    private Guid _otherBoardId;

    public ComputedFieldsIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

        _accountId = Guid.NewGuid();
        _userId = Guid.NewGuid();
        var workspace = Workspace.Create(_accountId, _userId, "Computed WS", $"computed-{Guid.NewGuid():N}", Now);
        _workspaceId = workspace.Id;
        _board = Board.Create(_accountId, workspace.Id, _userId, "Computed Board", null, Now, itemKeyPrefix: "CMP");
        _group = BoardGroup.Create(_accountId, workspace.Id, _board.Id, "Group", Color.Create("#808080"), FractionalIndex.Create("a0"), _userId, Now);
        _effort = Field(workspace.Id, _board.Id, "Effort", FieldType.Number, "{}", "a1");
        _double = Field(workspace.Id, _board.Id, "Double", FieldType.Formula, """{"expression":"[Effort]*2"}""", "a2");
        _ratio = Field(workspace.Id, _board.Id, "Ratio", FieldType.Formula, """{"expression":"[Effort]/([Effort]-[Effort])"}""", "a3");
        _sum = Field(workspace.Id, _board.Id, "Sub effort", FieldType.Rollup, $$"""{"function":"sum","fieldId":"{{_effort.Id}}"}""", "a4");
        _count = Field(workspace.Id, _board.Id, "Sub count", FieldType.Rollup, """{"function":"count"}""", "a5");

        _parent = Root("Parent", "a0");
        SetNumber(_parent, 5);
        _childA = Child("A", "a1");
        SetNumber(_childA, 3);
        _childB = Child("B", "a2");
        SetNumber(_childB, 4);
        _archivedChild = Child("Archived", "a3");
        SetNumber(_archivedChild, 100);
        _archivedChild.Archive(_userId, Now);

        // A second workspace owning a board with the same field names and other values.
        var other = Workspace.Create(_accountId, _userId, "Other WS", $"other-{Guid.NewGuid():N}", Now);
        _otherWorkspaceId = other.Id;
        var otherBoard = Board.Create(_accountId, other.Id, _userId, "Other Board", null, Now, itemKeyPrefix: "OTH");
        _otherBoardId = otherBoard.Id;
        var otherGroup = BoardGroup.Create(_accountId, other.Id, otherBoard.Id, "G", Color.Create("#808080"), FractionalIndex.Create("a0"), _userId, Now);
        var otherEffort = Field(other.Id, otherBoard.Id, "Effort", FieldType.Number, "{}", "a1");
        var otherDouble = Field(other.Id, otherBoard.Id, "Double", FieldType.Formula, """{"expression":"[Effort]*2"}""", "a2");
        var (sequence, key) = otherBoard.GenerateNextItemIdentity(_userId, Now);
        var otherItem = BoardItem.CreateRoot(_accountId, other.Id, otherBoard.Id, otherGroup.Id, "Other item", FractionalIndex.Create("a0"), _userId, Now, key, sequence);
        otherItem.UpdateFieldValue(otherEffort, FieldValue.Create(JsonValue.Create("999")), _userId, Now);

        await using var seed = _db.CreateOwnerContext(SystemTenant());
        seed.Workspaces.AddRange(workspace, other);
        seed.Boards.AddRange(_board, otherBoard);
        seed.BoardGroups.AddRange(_group, otherGroup);
        seed.BoardFields.AddRange(_effort, _double, _ratio, _sum, _count, otherEffort, otherDouble);
        seed.BoardItems.AddRange(_parent, _childA, _childB, _archivedChild, otherItem);
        await seed.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Formula_ShowsTheNumber_AndFollowsASourceEdit()
    {
        var first = (await ParentAsync()).ComputedValues![_double.Id.ToString()];
        first.State.Should().Be("ok");
        first.Value!.Value.GetDecimal().Should().Be(10m);

        await using (var edit = _db.CreateOwnerContext(Tenant(_workspaceId)))
        {
            var item = await edit.BoardItems.Include(i => i.FieldValues).SingleAsync(i => i.Id == _parent.Id);
            item.UpdateFieldValue(_effort, FieldValue.Create(JsonValue.Create("8")), _userId, Now.AddMinutes(1));
            await edit.SaveChangesAsync();
        }

        var after = await ParentAsync();
        after.ComputedValues![_double.Id.ToString()].Value!.Value.GetDecimal().Should().Be(16m);
        after.FieldValues![_effort.Id.ToString()].GetDecimal().Should().Be(8m, "the stored value is read next to the computed ones");
    }

    [Fact]
    public async Task Rollup_SumsLiveSubItems_AndCountsThem()
    {
        var parent = await ParentAsync();

        parent.ComputedValues![_sum.Id.ToString()].Value!.Value.GetDecimal().Should().Be(7m, "the archived sub-item is not rolled up");
        parent.ComputedValues[_count.Id.ToString()].Value!.Value.GetDecimal().Should().Be(2m);
    }

    [Fact]
    public async Task Formula_ThatCannotBeComputed_IsAnExplicitError()
    {
        var ratio = (await ParentAsync()).ComputedValues![_ratio.Id.ToString()];

        ratio.State.Should().Be("error");
        ratio.ErrorCode.Should().Be("divide-by-zero");
        ratio.Value.Should().BeNull();
    }

    [Fact]
    public async Task Cycle_IsRejectedOnSave_WithItsRuleCode()
    {
        var a = Field(_workspaceId, _board.Id, "Loop A", FieldType.Formula, """{"expression":"[Loop B]+1"}""", "a6");
        var b = Field(_workspaceId, _board.Id, "Loop B", FieldType.Formula, """{"expression":"[Effort]"}""", "a7");
        await using (var seed = _db.CreateOwnerContext(SystemTenant()))
        {
            seed.BoardFields.AddRange(a, b);
            await seed.SaveChangesAsync();
        }

        await using var context = _db.CreateOwnerContext(Tenant(_workspaceId));
        var field = await context.BoardFields.SingleAsync(f => f.Id == b.Id);
        field.UpdateSettings(FieldSettings.Create(JsonValue.Create("""{"expression":"[Loop A]+1"}""")), _userId, Now);

        var act = () => ComputedFieldGuard.EnsureAsync(context, field, CancellationToken.None);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkManagementRuleCodes.WorkManagement_Field_ComputedFieldCycle);
    }

    [Fact]
    public async Task OtherWorkspaces_AreNotRead_AndNeverLeakIntoTheValues()
    {
        // The board of another workspace is invisible under this workspace's tenant context.
        await using (var context = _db.CreateOwnerContext(Tenant(_workspaceId)))
        {
            var read = () => Handler(context, _workspaceId).Handle(new GetFullBoardQuery(_otherBoardId), CancellationToken.None);
            await read.Should().ThrowAsync<NotFoundException>();
        }

        // And this board's values never include the other board's Effort (999) or its doubled value.
        var board = await FullBoardAsync();
        var items = board.Lists.SelectMany(l => l.BoardItems).ToList();
        items.SelectMany(i => i.ComputedValues!.Values).Where(v => v.Value is not null)
            .Select(v => v.Value!.Value.ValueKind == System.Text.Json.JsonValueKind.Number ? v.Value.Value.GetDecimal() : 0m)
            .Should().NotContain([999m, 1998m]);
    }

    private async Task<FullBoardDto> FullBoardAsync()
    {
        await using var context = _db.CreateOwnerContext(Tenant(_workspaceId));
        var result = await Handler(context, _workspaceId).Handle(new GetFullBoardQuery(_board.Id), CancellationToken.None);
        result.Succeeded.Should().BeTrue();
        return result.Data!;
    }

    private async Task<BoardItemSummaryDto> ParentAsync() =>
        (await FullBoardAsync()).Lists.SelectMany(l => l.BoardItems).Single(i => i.Id == _parent.Id);

    private GetFullBoardQueryHandler Handler(Notrelix.Infrastructure.Data.ApplicationDbContext context, Guid workspaceId)
    {
        var visibility = new Mock<IResourceVisibility>();
        visibility.Setup(v => v.VisibleSpaceIdsAsync(workspaceId, _userId, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var actors = new Mock<IActorLookupService>();
        actors.Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<ActorSnapshot>());
        var collaboration = new Mock<IWorkManagementCollaborationReadPort>();
        collaboration.Setup(p => p.GetCountsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                (IReadOnlyDictionary<Guid, WorkItemCollaborationCounts>)ids.ToDictionary(id => id, _ => new WorkItemCollaborationCounts(0, 0)));
        var request = new FakeCurrentRequestContext().AsUser(_userId);
        request.Tenant.SetWorkspace(_accountId, workspaceId, _userId);
        return new GetFullBoardQueryHandler(context, actors.Object, collaboration.Object, visibility.Object, request);
    }

    private BoardField Field(Guid workspaceId, Guid boardId, string name, FieldType type, string settings, string position) =>
        BoardField.Create(_accountId, workspaceId, boardId, name, type, FieldSettings.Create(JsonValue.Create(settings)), FractionalIndex.Create(position), _userId, Now);

    private void SetNumber(BoardItem item, int value) =>
        item.UpdateFieldValue(_effort, FieldValue.Create(JsonValue.Create(value.ToString())), _userId, Now);

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

    private ICurrentTenantContext Tenant(Guid workspaceId)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetWorkspace(_accountId, workspaceId, _userId);
        return tenant;
    }

    private static ICurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }
}
