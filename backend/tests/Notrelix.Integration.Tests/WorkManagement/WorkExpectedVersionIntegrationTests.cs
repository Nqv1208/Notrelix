using Microsoft.Extensions.Logging;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Requests;
using Notrelix.Domain.Common;
using Notrelix.Application.Features.WorkManagement.BoardGroups.Commands.UpdateBoardGroup;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.MoveBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Application.Features.WorkManagement.Checklists.Commands.UpdateChecklistItem;
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
/// CONC-STALE-001 (WM-V3-CONC-001/002). Real P3 handlers run inside the real
/// <see cref="EfRequestDataSession"/> with the expected-version constraint the
/// pipeline derives from the command itself, on PostgreSQL. The version a read
/// returns lets a writer commit; a stale version fails as a 412 precondition and
/// rolls the whole mutation back; a stale no-op still fails; and a ChecklistItem
/// mutation binds to its owning Checklist's version.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkExpectedVersionIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 10, 10, 0, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private Guid _accountId;
    private Guid _workspaceId;
    private Guid _userId;
    private Guid _todoId;
    private Guid _doneId;
    private Guid _itemId;
    private Guid _checklistItemId;
    private Guid _checklistId;

    public WorkExpectedVersionIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync()
    {
        await new DatabaseReset(_db.ConnectionString).ResetAsync();
        _accountId = Guid.NewGuid();
        _userId = Guid.NewGuid();
        var workspace = Workspace.Create(_accountId, _userId, "Version WS", $"ver-{Guid.NewGuid():N}", Now);
        var board = Board.Create(_accountId, workspace.Id, _userId, "Version Board", null, Now);
        var todo = BoardGroup.Create(_accountId, workspace.Id, board.Id, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), _userId, Now);
        var done = BoardGroup.Create(_accountId, workspace.Id, board.Id, "Done", Color.Create("#808080"), FractionalIndex.Create("a1"), _userId, Now);
        var item = BoardItem.CreateRoot(_accountId, workspace.Id, board.Id, todo.Id, "Task", FractionalIndex.Create("a0"), _userId, Now);
        var checklist = Checklist.Create(_accountId, workspace.Id, item.Id, "Checklist", FractionalIndex.Create("a0"), _userId, Now);
        var checklistItem = checklist.AddItem("Step", FractionalIndex.Create("a0"), _userId, Now);
        (_workspaceId, _todoId, _doneId, _itemId, _checklistId, _checklistItemId) = (workspace.Id, todo.Id, done.Id, item.Id, checklist.Id, checklistItem.Id);

        await using var seed = _db.CreateContext(SystemTenant());
        seed.Workspaces.Add(workspace);
        seed.Boards.Add(board);
        seed.BoardGroups.AddRange(todo, done);
        seed.BoardItems.Add(item);
        seed.Checklists.Add(checklist);
        await seed.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MoveItem_WithTheReadVersion_Commits_AndWithAStaleVersion_FailsWithoutMutation()
    {
        var readVersion = await VersionAsync<BoardItem>(_itemId);

        await MoveAsync(_doneId, readVersion);
        var afterMove = await VersionAsync<BoardItem>(_itemId);
        afterMove.Should().Be(readVersion + 1);

        var stale = () => MoveAsync(_todoId, readVersion);

        (await stale.Should().ThrowAsync<PreconditionFailedException>()).Which.ErrorCode.Should().Be("common.precondition-failed");
        await using var verify = _db.CreateContext(SystemTenant());
        var item = await verify.BoardItems.SingleAsync(i => i.Id == _itemId);
        item.GroupId.Should().Be(_doneId, "the stale move rolls back entirely");
        item.Version.Should().Be(afterMove);
    }

    [Fact]
    public async Task StaleVersion_OnASemanticNoOp_StillFailsThePrecondition()
    {
        var group = await VersionAsync<BoardGroup>(_todoId);

        var staleNoOp = () => RunAsync(new UpdateBoardGroupCommand(_todoId, "Todo", group - 1), (context, command) =>
            new UpdateBoardGroupCommandHandler(context, CurrentUser(), Clock()).Handle(command, CancellationToken.None));

        await staleNoOp.Should().ThrowAsync<PreconditionFailedException>(
            "a stale writer is told it is stale even when its change would have been a no-op");
    }

    [Fact]
    public async Task ChecklistItemMutation_BindsToTheOwningChecklistVersion()
    {
        var checklistVersion = await VersionAsync<Checklist>(_checklistId);

        await PatchChecklistItemAsync(checklistVersion, isChecked: true);
        (await VersionAsync<Checklist>(_checklistId)).Should().Be(checklistVersion + 1);

        var stale = () => PatchChecklistItemAsync(checklistVersion, isChecked: false);

        await stale.Should().ThrowAsync<PreconditionFailedException>();
        await using var verify = _db.CreateContext(SystemTenant());
        (await verify.ChecklistItems.SingleAsync(i => i.Id == _checklistItemId)).IsDone.Should().BeTrue();
    }

    private Task MoveAsync(Guid groupId, long expectedVersion) =>
        RunAsync(new MoveBoardItemCommand(_itemId, groupId, expectedVersion), (context, command) =>
            new MoveBoardItemCommandHandler(new MoveBoardItemUseCase(context, Clock(), new WorkPlacementResolver(new PostgresWorkOrderingScopeLock(context))), CurrentUser())
                .Handle(command, CancellationToken.None));

    private Task PatchChecklistItemAsync(long expectedVersion, bool isChecked) =>
        RunAsync(new UpdateChecklistItemCommand(_checklistItemId, expectedVersion, IsChecked: isChecked), (context, command) =>
            new UpdateChecklistItemCommandHandler(context, CurrentUser(), Clock()).Handle(command, CancellationToken.None));

    /// <summary>Runs the handler inside the real data session with the constraint the pipeline derives from the command.</summary>
    private async Task RunAsync<TCommand, TResponse>(TCommand command, Func<ApplicationDbContext, TCommand, Task<TResponse>> handle)
        where TCommand : IExpectedVersionRequest
    {
        await using var context = _db.CreateContext(Tenant());
        var session = new EfRequestDataSession(context, Mock.Of<IRlsSessionContext>(), Mock.Of<ILogger<EfRequestDataSession>>());
        var constraint = new ExpectedVersionConstraint(typeof(TCommand), command.Resource, command.ExpectedVersion);
        await session.ExecuteAsync(
            new RequestDataSessionOptions(RequestDataAccess.Transactional, ApplyTenantScope: false, ApplyResourceScope: false, ExpectedVersion: constraint),
            _ => handle(context, command),
            CancellationToken.None);
    }

    private async Task<long> VersionAsync<TAggregate>(Guid id) where TAggregate : AggregateRoot
    {
        await using var context = _db.CreateContext(SystemTenant());
        return await context.Set<TAggregate>().Where(a => a.Id == id).Select(a => a.Version).SingleAsync();
    }

    private ICurrentUser CurrentUser()
    {
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(u => u.UserId).Returns(_userId);
        return currentUser.Object;
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
