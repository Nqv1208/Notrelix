using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Features.WorkManagement.Boards.Commands.RestoreBoard;
using Notrelix.Application.Features.WorkManagement.Boards.Queries.ListDeletedBoards;
using Notrelix.Application.Features.WorkManagement.Common.DTOs;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Labels;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Infrastructure.BackgroundJobs;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Infrastructure.Identity.Services;

namespace Notrelix.Integration.Tests.Workspaces.OrgTree;

/// <summary>
/// UC-WM-A07 on PostgreSQL through the real pipeline, facts query and engine: the deleted-boards list shows a
/// board to whoever restore lets restore it (a workspace Owner, a Space manager of the board's Space, the deleter, a
/// board Owner or Admin) and to no one else, narrows to one Space, drops a board past its 30 days; and the purge
/// (UC-WM-B17 for boards) removes a board deleted more than 30 days ago with its children, as the worker login with
/// the RLS policy applied, and leaves one inside the window.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class DeletedBoardsIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public DeletedBoardsIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        // Boards are created at BaseTime and deleted then (Yesterday); the requests run a day later.
        _host = new OrgTreeTestHost(db) { Now = OrgTreeTestHost.BaseTime.AddDays(1) };
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static readonly DateTimeOffset Yesterday = OrgTreeTestHost.BaseTime;
    private static readonly DateTimeOffset PurgeTime = OrgTreeTestHost.BaseTime.AddDays(32);

    [Fact]
    public async Task WorkspaceOwner_SeesABoardSomeoneElseDeleted_AndAPlainMemberDoesNot()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var deleter = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Deleter");
        var bystander = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Bystander");
        var board = await _host.SeedBoardAsync(stack, "Roadmap", transition: b => b.Delete(deleter, Yesterday));
        // The owner holds no board role on it, so only the engine's restore decision can list it.
        await _host.WithSystemContextAsync(c => c.BoardMembers.Where(m => m.BoardId == board).ExecuteDeleteAsync());

        (await ListAsync(stack, stack.OwnerId)).Should().ContainSingle(b => b.Id == board)
            .Which.RestorableUntil.Should().Be(Yesterday.AddDays(30).UtcDateTime);
        (await ListAsync(stack, bystander)).Should().BeEmpty("a member who may not restore it gets nothing, not a marked row");
    }

    [Fact]
    public async Task SpaceManager_SeesAndRestoresABoardOfTheirSpace_AndTheSpaceFilterNarrows()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var lead = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Lead");
        var space = await _host.SeedSpaceAsync(stack, "Kỹ thuật", SpaceVisibility.Private, stack.OwnerId,
            transition: s => s.AddMember(SpaceMemberSubjectType.User, lead, SpaceRole.Admin, stack.OwnerId, OrgTreeTestHost.BaseTime));
        var other = await _host.SeedSpaceAsync(stack, "Kinh doanh", SpaceVisibility.Private, stack.OwnerId);
        var board = await _host.SeedBoardAsync(stack, "Sprint", spaceId: space, transition: b => b.Delete(stack.OwnerId, Yesterday));
        await _host.WithSystemContextAsync(c => c.BoardMembers.Where(m => m.BoardId == board).ExecuteDeleteAsync());

        (await ListAsync(stack, lead)).Should().ContainSingle(b => b.Id == board).Which.SpaceId.Should().Be(space);
        (await ListAsync(stack, lead, space)).Should().ContainSingle(b => b.Id == board);
        (await ListAsync(stack, lead, other)).Should().BeEmpty();

        var version = await _host.ReadAsync(c => c.Boards.IgnoreQueryFilters().Where(b => b.Id == board).Select(b => b.Version).SingleAsync());
        using (var provider = _host.CreateProvider(stack.AccountId, lead))
            await OrgTreeTestHost.SendIdempotentAsync(provider, new RestoreBoardCommand(board, version));

        (await _host.ReadAsync(c => c.Boards.IgnoreQueryFilters().Where(b => b.Id == board).Select(b => b.DeletedAt).SingleAsync()))
            .Should().BeNull("the Space manager the list showed it to may restore it");
        (await ListAsync(stack, lead)).Should().BeEmpty();
    }

    [Fact]
    public async Task ABoardPastItsThirtyDays_IsNotListed_EvenToTheWorkspaceOwner()
    {
        var stack = await _host.SeedWorkspaceAsync();
        await _host.SeedBoardAsync(stack, "Old", transition: b => b.Delete(stack.OwnerId, Yesterday));
        _host.Now = OrgTreeTestHost.BaseTime.AddDays(31);

        (await ListAsync(stack, stack.OwnerId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Purge_RemovesABoardDeletedMoreThanThirtyDaysAgo_WithItsChildren_AndKeepsOneInsideTheWindow()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var expired = await SeedFullBoardAsync(stack, "Expired", OrgTreeTestHost.BaseTime);
        var recent = await SeedFullBoardAsync(stack, "Recent", OrgTreeTestHost.BaseTime.AddDays(5));
        var active = await SeedFullBoardAsync(stack, "Active", null);

        (await PurgeAsync()).Should().Be(1);

        (await CountAsync(expired)).Should().Be(new BoardRows(0, 0, 0, 0, 0, 0));
        (await CountAsync(recent)).Should().Be(new BoardRows(1, 1, 1, 1, 1, 1));
        (await CountAsync(active)).Should().Be(new BoardRows(1, 1, 1, 1, 1, 1));
        (await _host.ReadAsync(c => c.EnterpriseAuditLogs
                .CountAsync(a => a.Action == "PurgeBoard" && a.ResourceId == expired && a.WorkspaceId == stack.WorkspaceId)))
            .Should().Be(1);

        (await PurgeAsync()).Should().Be(0, "a purged board is gone, so a second pass changes nothing");
    }

    [Fact]
    public void PurgeOptions_AreOffByDefault_AndValidated()
    {
        var validator = new DeletedBoardPurgeOptionsValidator();
        new DeletedBoardPurgeOptions().Enabled.Should().BeFalse();
        validator.Validate(null, new DeletedBoardPurgeOptions()).Succeeded.Should().BeTrue();
        validator.Validate(null, new DeletedBoardPurgeOptions { Interval = TimeSpan.FromSeconds(1) }).Failed.Should().BeTrue();
        validator.Validate(null, new DeletedBoardPurgeOptions { BatchSize = 0 }).Failed.Should().BeTrue();
    }

    // ───────────────────────── harness ─────────────────────────

    private sealed record BoardRows(int Boards, int Groups, int Items, int Fields, int Values, int ItemLabels);

    private async Task<List<RetiredBoardDto>> ListAsync(OrgStack stack, Guid userId, Guid? spaceId = null)
    {
        using var provider = _host.CreateProvider(stack.AccountId, userId);
        return await OrgTreeTestHost.QueryAsync(provider, new ListDeletedBoardsQuery(stack.WorkspaceId, spaceId));
    }

    /// <summary>A board with a group, an item, a number field and its value (a blocking key into the field), a label on the item.</summary>
    private async Task<Guid> SeedFullBoardAsync(OrgStack stack, string title, DateTimeOffset? deletedAt)
    {
        var board = await _host.SeedBoardAsync(stack, title, transition: b =>
        {
            if (deletedAt is { } at) b.Delete(stack.OwnerId, at);
        });
        var time = OrgTreeTestHost.BaseTime;
        await _host.WithSystemContextAsync(c =>
        {
            var group = BoardGroup.Create(stack.AccountId, stack.WorkspaceId, board, "Group", Color.Create("#0079BF"), FractionalIndex.Create("a0"), stack.OwnerId, time);
            var item = BoardItem.CreateRoot(stack.AccountId, stack.WorkspaceId, board, group.Id, "Item", FractionalIndex.Create("a0"), stack.OwnerId, time);
            var field = BoardField.Create(stack.AccountId, stack.WorkspaceId, board, "Effort", FieldType.Number, FieldSettings.Empty(), FractionalIndex.Create("a0"), stack.OwnerId, time);
            var label = Label.Create(stack.AccountId, stack.WorkspaceId, board, "Bug", LabelColor.Create("#E53935"), stack.OwnerId, time);
            c.BoardGroups.Add(group);
            c.BoardItems.Add(item);
            c.BoardFields.Add(field);
            c.Labels.Add(label);
            c.BoardItemValues.Add(BoardItemValue.Create(item.Id, field.Id, FieldValue.Create(JsonValue.Create("3")!)));
            c.BoardItemLabels.Add(BoardItemLabel.Create(stack.AccountId, stack.WorkspaceId, board, item.Id, label.Id, stack.OwnerId, time));
            return Task.CompletedTask;
        });
        return board;
    }

    private Task<BoardRows> CountAsync(Guid board) =>
        _host.ReadAsync(async c => new BoardRows(
            await c.Boards.IgnoreQueryFilters().CountAsync(b => b.Id == board),
            await c.BoardGroups.IgnoreQueryFilters().CountAsync(g => g.BoardId == board),
            await c.BoardItems.IgnoreQueryFilters().CountAsync(i => i.BoardId == board),
            await c.BoardFields.IgnoreQueryFilters().CountAsync(f => f.BoardId == board),
            await c.BoardItemValues.IgnoreQueryFilters().CountAsync(v => c.BoardFields.IgnoreQueryFilters().Any(f => f.Id == v.FieldId && f.BoardId == board)),
            await c.BoardItemLabels.IgnoreQueryFilters().CountAsync(l => l.BoardId == board)));

    private async Task<int> PurgeAsync()
    {
        var tenant = new CurrentTenantContext();
        await using var context = PostgresTestContainer.CreateContextFor(_db.WorkerConnectionString, tenant);
        var rls = new RlsSessionContext(context, Options.Create(new RlsOptions { Enabled = true, SetSessionContext = true }), tenant);
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(PurgeTime);
        var purger = new DeletedBoardPurger(
            context, tenant, rls, clock.Object,
            Options.Create(new DeletedBoardPurgeOptions { Enabled = true, BatchSize = 10 }),
            NullLogger<DeletedBoardPurger>.Instance);
        return await purger.RunOnceAsync(default);
    }
}
