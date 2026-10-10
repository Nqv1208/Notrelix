using Notrelix.Application.Features.WorkManagement.Checklists.Commands.CreateChecklist;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.CreateChecklistItem;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.DeleteChecklist;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.DeleteChecklistItem;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.MoveChecklist;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.MoveChecklistItem;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.UpdateChecklistItem;
using Notrelix.Application.Features.WorkManagement.Checklists.Queries.GetChecklists;
using Notrelix.Application.Features.WorkManagement.Ordering;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Checklists;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.WorkManagement;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// CHK-APP-001 (WM-V3-APP-CHK-001). Every checklist and checklist-item mutation runs
/// through the real handlers, the Checklist aggregate and the PostgreSQL ordering
/// lock, each request in a fresh context and transaction. The baseline added items
/// straight to the DbSet with key "a0", and its PATCH toggled instead of setting
/// the desired state (and could not find the item at all from a fresh request).
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkChecklistIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private Guid _accountId;
    private Guid _workspaceId;
    private Guid _userId;
    private Guid _boardItemId;

    public WorkChecklistIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        await new DatabaseReset(_db.ConnectionString).ResetAsync();

        _accountId = Guid.NewGuid();
        _userId = Guid.NewGuid();
        var workspace = Workspace.Create(_accountId, _userId, "Checklist WS", $"chk-{Guid.NewGuid():N}", Now);
        var board = Board.Create(_accountId, workspace.Id, _userId, "Checklist Board", null, Now);
        var group = BoardGroup.Create(_accountId, workspace.Id, board.Id, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), _userId, Now);
        var item = BoardItem.CreateRoot(_accountId, workspace.Id, board.Id, group.Id, "Task", FractionalIndex.Create("a0"), _userId, Now);
        (_workspaceId, _boardItemId) = (workspace.Id, item.Id);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Workspaces.Add(workspace);
        seed.Boards.Add(board);
        seed.BoardGroups.Add(group);
        seed.BoardItems.Add(item);
        await seed.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Checklists_AreCreatedAndMovedRelativeToTheirNeighbors()
    {
        var a = await CreateChecklistAsync("A");
        var b = await CreateChecklistAsync("B");
        var c = await CreateChecklistAsync("C", previous: a, next: b);
        (await ChecklistOrderAsync()).Should().Equal(a, c, b);

        await InTransactionAsync(context => new MoveChecklistCommandHandler(context, CurrentUser(), Clock(), Resolver(context))
            .Handle(new MoveChecklistCommand(a, b, null), CancellationToken.None));

        (await ChecklistOrderAsync()).Should().Equal(c, b, a);
    }

    [Fact]
    public async Task Items_AreAddedThroughTheChecklist_WithDistinctOrderedKeys_AndMoved()
    {
        var checklist = await CreateChecklistAsync("Launch");
        var versionBefore = await ChecklistVersionAsync(checklist);
        var first = await AddItemAsync(checklist, "First");
        var second = await AddItemAsync(checklist, "Second");
        var middle = await AddItemAsync(checklist, "Middle", previous: first, next: second);

        (await ItemOrderAsync(checklist)).Should().Equal(first, middle, second);
        (await ChecklistVersionAsync(checklist)).Should().Be(versionBefore + 3, "each item add versions the owning checklist once");

        await InTransactionAsync(context => new MoveChecklistItemCommandHandler(context, CurrentUser(), Clock(), Resolver(context))
            .Handle(new MoveChecklistItemCommand(first, second, null), CancellationToken.None));

        (await ItemOrderAsync(checklist)).Should().Equal(middle, second, first);
    }

    [Fact]
    public async Task ItemPatch_SetsTheDesiredState_FromAFreshRequest_AndRepeatingIsANoOp()
    {
        var checklist = await CreateChecklistAsync("Launch");
        var item = await AddItemAsync(checklist, "Write notes");

        await PatchItemAsync(item, title: "Publish notes", isChecked: true);
        var versionAfterFirst = await ChecklistVersionAsync(checklist);
        await PatchItemAsync(item, title: null, isChecked: true);

        var dto = (await GetChecklistsAsync()).Single();
        dto.Items.Single().Title.Should().Be("Publish notes");
        dto.Items.Single().Status.Should().Be(nameof(ChecklistItemStatus.Done), "a repeated desired state must not invert the item");
        dto.Version.Should().Be(versionAfterFirst, "repeating the current state is a semantic no-op");
    }

    [Fact]
    public async Task Deletes_RunThroughTheAggregate_AndADeletedChecklistLeavesTheReadSurface()
    {
        var keep = await CreateChecklistAsync("Keep");
        var drop = await CreateChecklistAsync("Drop");
        var item = await AddItemAsync(keep, "Remove me");
        await AddItemAsync(drop, "Owned by the deleted checklist");

        await InTransactionAsync(context => new DeleteChecklistItemCommandHandler(context, CurrentUser(), Clock())
            .Handle(new DeleteChecklistItemCommand(item), CancellationToken.None));
        await InTransactionAsync(context => new DeleteChecklistCommandHandler(context, CurrentUser(), Clock())
            .Handle(new DeleteChecklistCommand(drop), CancellationToken.None));

        var dtos = await GetChecklistsAsync();
        dtos.Should().ContainSingle().Which.Id.Should().Be(keep);
        dtos.Single().Items.Should().BeEmpty();
        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.Checklists.IgnoreQueryFilters().SingleAsync(c => c.Id == drop)).DeletedAt.Should().NotBeNull(
            "deleting a checklist is the aggregate's soft delete, not a row removal");
    }

    private async Task<Guid> CreateChecklistAsync(string title, Guid? previous = null, Guid? next = null) =>
        (await InTransactionAsync(context => new CreateChecklistCommandHandler(context, Request(), Clock(), Resolver(context))
            .Handle(new CreateChecklistCommand(_boardItemId, title, previous, next), CancellationToken.None))).Data;

    private async Task<Guid> AddItemAsync(Guid checklistId, string title, Guid? previous = null, Guid? next = null) =>
        (await InTransactionAsync(context => new CreateChecklistItemCommandHandler(context, CurrentUser(), Clock(), Resolver(context))
            .Handle(new CreateChecklistItemCommand(checklistId, title, previous, next), CancellationToken.None))).Data;

    private Task PatchItemAsync(Guid itemId, string? title, bool? isChecked) =>
        InTransactionAsync(context => new UpdateChecklistItemCommandHandler(context, CurrentUser(), Clock())
            .Handle(new UpdateChecklistItemCommand(itemId, title, isChecked), CancellationToken.None));

    private async Task<List<Notrelix.Application.Features.WorkManagement.Common.DTOs.ChecklistDto>> GetChecklistsAsync()
    {
        await using var context = _db.CreateContext(Tenant());
        return (await new GetChecklistsQueryHandler(context).Handle(new GetChecklistsQuery(_boardItemId), CancellationToken.None)).Data!;
    }

    private async Task<List<Guid>> ChecklistOrderAsync() =>
        (await GetChecklistsAsync()).Select(c => c.Id).ToList();

    private async Task<List<Guid>> ItemOrderAsync(Guid checklistId) =>
        (await GetChecklistsAsync()).Single(c => c.Id == checklistId).Items.Select(i => i.Id).ToList();

    private async Task<long> ChecklistVersionAsync(Guid checklistId)
    {
        await using var context = _db.CreateContext(SystemTenant());
        return await context.Checklists.Where(c => c.Id == checklistId).Select(c => c.Version).SingleAsync();
    }

    private async Task<T> InTransactionAsync<T>(Func<ApplicationDbContext, Task<T>> work)
    {
        await using var context = _db.CreateContext(Tenant());
        await using var transaction = await context.Database.BeginTransactionAsync();
        var result = await work(context);
        await context.SaveChangesAsync();
        await transaction.CommitAsync();
        return result;
    }

    private static WorkPlacementResolver Resolver(ApplicationDbContext context) => new(new PostgresWorkOrderingScopeLock(context));

    private static IDateTimeProvider Clock() => FakeDateTimeProvider.WithFixedTime(Now);

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
