using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.WorkManagement.Ordering;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.WorkManagement;
using Notrelix.Application.Features.WorkManagement.Approvals.Queries.ListApprovalRequests;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.AddFieldOption;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.MoveFieldOption;
using Notrelix.Application.Features.WorkManagement.BoardFields.Commands.UpdateFieldOption;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.ArchiveBoardItem;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.BulkUpdateBoardItems;
using Notrelix.Application.Features.WorkManagement.BoardItems.Commands.UnarchiveBoardItem;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Approvals;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;
using PreconditionFailed = Notrelix.Application.Common.Exceptions.PreconditionFailedException;

namespace Notrelix.Integration.Tests.WorkManagement;

/// <summary>
/// Work Management acceptance gaps on PostgreSQL through the real pipeline (NRX-003, NRX-004, NRX-007,
/// NRX-010): archive refuses a stale version and changes nothing, and answers people without access with the
/// same NotFound as a missing item (UC-WM-B09); a bulk change retried with the same key has one effect
/// (UC-WM-B36); the option commands refuse a stale field version and a retried add has one effect
/// (UC-WM-C07); the board approval list reads for a viewer and is NotFound for anyone outside the Workspace
/// (UC-WM-B28).
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkAcceptanceGapsPipelineIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public WorkAcceptanceGapsPipelineIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Archive_WithAStaleVersion_IsRefusedAndTheItemStaysLive()
    {
        var (stack, _, items) = await SeedBoardWithItemsAsync(1);
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var read = await VersionAsync(items[0]);
        await OrgTreeTestHost.SendAsync(owner, new ArchiveBoardItemCommand(items[0], read));
        await OrgTreeTestHost.SendAsync(owner, new UnarchiveBoardItemCommand(items[0], await VersionAsync(items[0])));
        var afterRoundTrip = await VersionAsync(items[0]);

        var stale = () => OrgTreeTestHost.SendAsync(owner, new ArchiveBoardItemCommand(items[0], read));

        await stale.Should().ThrowAsync<PreconditionFailed>("the writer last read the item before two other changes");
        (await ItemAsync(items[0])).IsArchived.Should().BeFalse();
        (await VersionAsync(items[0])).Should().Be(afterRoundTrip, "a refused write changes nothing");
    }

    [Fact]
    public async Task Archive_ByAMemberWhoCannotReadThePrivateBoard_AnswersLikeAMissingItem()
    {
        var (stack, _, items) = await SeedBoardWithItemsAsync(1, BoardVisibility.Private);
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var version = await VersionAsync(items[0]);
        using var provider = _host.CreateProvider(stack.AccountId, member);

        var hidden = () => OrgTreeTestHost.SendAsync(provider, new ArchiveBoardItemCommand(items[0], version));
        var missing = () => OrgTreeTestHost.SendAsync(provider, new ArchiveBoardItemCommand(Guid.CreateVersion7(), version));

        var hiddenError = (await hidden.Should().ThrowAsync<AppNotFound>()).Which;
        var missingError = (await missing.Should().ThrowAsync<AppNotFound>()).Which;
        hiddenError.GetType().Should().Be(missingError.GetType());
        (await ItemAsync(items[0])).IsArchived.Should().BeFalse();
        (await VersionAsync(items[0])).Should().Be(version);
    }

    [Fact]
    public async Task Archive_ByAPersonOutsideTheWorkspace_IsDeniedAndChangesNothing()
    {
        var (stack, _, items) = await SeedBoardWithItemsAsync(1);
        var outsider = await _host.AddOutsiderAsync(stack);
        var otherWorkspace = await _host.SeedWorkspaceAsync();
        var version = await VersionAsync(items[0]);

        foreach (var (user, account) in new[] { (outsider, stack.AccountId), (otherWorkspace.OwnerId, otherWorkspace.AccountId) })
        {
            using var provider = _host.CreateProvider(account, user);
            var act = () => OrgTreeTestHost.SendAsync(provider, new ArchiveBoardItemCommand(items[0], version));
            await act.Should().ThrowAsync<Exception>()
                .Where(e => e is AppForbidden || e is AppNotFound, "a person with no role in the Workspace never reaches the item");
        }

        (await ItemAsync(items[0])).IsArchived.Should().BeFalse();
        (await VersionAsync(items[0])).Should().Be(version);
    }

    [Fact]
    public async Task BulkUpdate_RetriedWithTheSameKey_HasOneEffectAndReplaysTheFirstResult()
    {
        var (stack, boardId, items) = await SeedBoardWithItemsAsync(2);
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var versions = await VersionsAsync(items);
        var due = new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc);
        var command = new BulkUpdateBoardItemsCommand(boardId, BulkBoardItemOperations.SetDueDate, items, versions, DueDate: due);
        var key = $"bulk-{Guid.NewGuid():N}";

        var first = (BulkUpdateBoardItemsResult)(await OrgTreeTestHost.SendIdempotentAsync(owner, command, key))!;
        var afterFirst = await VersionsAsync(items);
        var replay = (BulkUpdateBoardItemsResult)(await OrgTreeTestHost.SendIdempotentAsync(owner, command, key))!;

        first.Updated.Should().Be(2);
        replay.Should().Be(first, "a replay returns the first committed result");
        (await VersionsAsync(items)).Should().BeEquivalentTo(afterFirst, "the replay bumped no version and wrote nothing");
        afterFirst.Keys.Should().OnlyContain(id => afterFirst[id] == versions[id] + 1, "one logical operation raised each version once");
    }

    [Fact]
    public async Task BulkUpdate_WithAStaleItem_RefusesTheWholeRequest()
    {
        var (stack, boardId, items) = await SeedBoardWithItemsAsync(2);
        using var owner = _host.CreateProvider(stack.AccountId, stack.OwnerId);
        var versions = await VersionsAsync(items);
        var staleVersions = new Dictionary<Guid, long>(versions) { [items[1]] = versions[items[1]] - 1 };
        var due = new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc);

        var act = () => OrgTreeTestHost.SendIdempotentAsync(owner,
            new BulkUpdateBoardItemsCommand(boardId, BulkBoardItemOperations.SetDueDate, items, staleVersions, DueDate: due));

        await act.Should().ThrowAsync<Exception>("one stale item refuses the request");
        (await VersionsAsync(items)).Should().BeEquivalentTo(versions, "no item changed");
    }

    [Fact]
    public async Task OptionCommands_RefuseAStaleFieldVersion_AndARetriedAddHasOneEffect()
    {
        var (stack, boardId, _) = await SeedBoardWithItemsAsync(0);
        var fieldId = await SeedSelectFieldAsync(stack, boardId);
        using var owner = Provider(stack);
        var key = $"option-{Guid.NewGuid():N}";
        var start = await FieldVersionAsync(fieldId);

        var add = new AddFieldOptionCommand(boardId, fieldId, "High", "#E5484D", start);
        var added = (Result<AddFieldOptionResult>)(await OrgTreeTestHost.SendIdempotentAsync(owner, add, key))!;
        var replay = (Result<AddFieldOptionResult>)(await OrgTreeTestHost.SendIdempotentAsync(owner, add, key))!;

        replay.Data!.OptionId.Should().Be(added.Data!.OptionId, "the retry replays the first result");
        (await OptionNamesAsync(fieldId)).Should().Equal("High");
        (await FieldVersionAsync(fieldId)).Should().Be(start + 1);

        var second = (Result<AddFieldOptionResult>)(await OrgTreeTestHost.SendIdempotentAsync(owner,
            new AddFieldOptionCommand(boardId, fieldId, "Low", "#30A46C", added.Data.Version, PreviousSiblingId: added.Data.OptionId)))!;
        second.Succeeded.Should().BeTrue();

        // The reader of `start` is now two versions behind: add, rename, move and a second add all refuse it.
        var staleAdd = () => OrgTreeTestHost.SendIdempotentAsync(owner, new AddFieldOptionCommand(boardId, fieldId, "Lost", "#808080", start));
        var staleRename = () => OrgTreeTestHost.SendIdempotentAsync(owner,
            new UpdateFieldOptionCommand(boardId, fieldId, added.Data.OptionId, "Lost", "#808080", start));
        var staleMove = () => OrgTreeTestHost.SendIdempotentAsync(owner,
            new MoveFieldOptionCommand(boardId, fieldId, second.Data!.OptionId, start, NextSiblingId: added.Data.OptionId));

        await staleAdd.Should().ThrowAsync<PreconditionFailed>();
        await staleRename.Should().ThrowAsync<PreconditionFailed>();
        await staleMove.Should().ThrowAsync<PreconditionFailed>();
        (await OptionNamesAsync(fieldId)).Should().Equal(["High", "Low"], "no stale write changed the options");
    }

    [Fact]
    public async Task OptionCommands_RenameAndMoveWithTheCurrentVersion_ChangeTheFieldOnce()
    {
        var (stack, boardId, _) = await SeedBoardWithItemsAsync(0);
        var fieldId = await SeedSelectFieldAsync(stack, boardId);
        using var owner = Provider(stack);
        var one = (Result<AddFieldOptionResult>)(await OrgTreeTestHost.SendIdempotentAsync(owner,
            new AddFieldOptionCommand(boardId, fieldId, "One", "#808080", await FieldVersionAsync(fieldId))))!;
        var two = (Result<AddFieldOptionResult>)(await OrgTreeTestHost.SendIdempotentAsync(owner,
            new AddFieldOptionCommand(boardId, fieldId, "Two", "#808080", one.Data!.Version, PreviousSiblingId: one.Data.OptionId)))!;

        await OrgTreeTestHost.SendIdempotentAsync(owner,
            new UpdateFieldOptionCommand(boardId, fieldId, one.Data.OptionId, "Uno", "#808080", await FieldVersionAsync(fieldId)));
        await OrgTreeTestHost.SendIdempotentAsync(owner,
            new MoveFieldOptionCommand(boardId, fieldId, two.Data!.OptionId, await FieldVersionAsync(fieldId), NextSiblingId: one.Data.OptionId));

        (await OptionNamesAsync(fieldId)).Should().Equal("Two", "Uno");
    }

    [Fact]
    public async Task ApprovalList_OfABoard_ReadsForAViewerNewestFirst()
    {
        var (stack, boardId, items) = await SeedBoardWithItemsAsync(1);
        var viewer = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Viewer");
        await SeedBoardApprovalsAsync(stack, boardId, items[0]);

        using var viewerProvider = _host.CreateProvider(stack.AccountId, viewer);
        var listed = await OrgTreeTestHost.QueryAsync(viewerProvider, new ListApprovalRequestsQuery(boardId));

        listed.Select(a => a.Title).Should().Equal("Second", "First");
    }

    [Fact]
    public async Task ApprovalList_OfAPrivateBoard_IsNotFoundToAMemberWhoCannotReadIt_AndDeniedOutsideTheWorkspace()
    {
        var (stack, boardId, items) = await SeedBoardWithItemsAsync(1, BoardVisibility.Private);
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var outsider = await _host.AddOutsiderAsync(stack);
        var otherWorkspace = await _host.SeedWorkspaceAsync();
        await SeedBoardApprovalsAsync(stack, boardId, items[0]);

        using var memberProvider = _host.CreateProvider(stack.AccountId, member);
        var hidden = () => OrgTreeTestHost.SendAsync(memberProvider, new ListApprovalRequestsQuery(boardId));
        var missing = () => OrgTreeTestHost.SendAsync(memberProvider, new ListApprovalRequestsQuery(Guid.CreateVersion7()));
        await hidden.Should().ThrowAsync<AppNotFound>("a Private board the member cannot read answers like a missing one");
        await missing.Should().ThrowAsync<AppNotFound>();

        foreach (var (user, account) in new[] { (outsider, stack.AccountId), (otherWorkspace.OwnerId, otherWorkspace.AccountId) })
        {
            using var provider = _host.CreateProvider(account, user);
            var act = () => OrgTreeTestHost.SendAsync(provider, new ListApprovalRequestsQuery(boardId));
            await act.Should().ThrowAsync<Exception>().Where(e => e is AppForbidden || e is AppNotFound);
        }
    }

    private Task SeedBoardApprovalsAsync(OrgStack stack, Guid boardId, Guid itemId) =>
        _host.WithSystemContextAsync(c =>
        {
            ResourceRef Board() => ResourceRef.Create(ResourceKind.Create("work-management.board"), boardId, stack.WorkspaceId);
            c.ApprovalRequests.Add(ApprovalRequest.Create(stack.AccountId, stack.WorkspaceId, Board(), "First", stack.OwnerId, OrgTreeTestHost.BaseTime));
            c.ApprovalRequests.Add(ApprovalRequest.Create(stack.AccountId, stack.WorkspaceId, Board(), "Second", stack.OwnerId, OrgTreeTestHost.BaseTime.AddMinutes(5)));
            // A request aimed at an item of the board is the item's, not the board's.
            c.ApprovalRequests.Add(ApprovalRequest.Create(stack.AccountId, stack.WorkspaceId,
                ResourceRef.Create(ResourceKind.Create("work-management.board-item"), itemId, stack.WorkspaceId), "On an item", stack.OwnerId, OrgTreeTestHost.BaseTime));
            return Task.CompletedTask;
        });

    private async Task<(OrgStack Stack, Guid BoardId, List<Guid> Items)> SeedBoardWithItemsAsync(
        int count, BoardVisibility visibility = BoardVisibility.Workspace)
    {
        var stack = await _host.SeedWorkspaceAsync();
        var boardId = await _host.SeedBoardAsync(stack, "Gaps", visibility);
        var group = BoardGroup.Create(stack.AccountId, stack.WorkspaceId, boardId, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), stack.OwnerId, OrgTreeTestHost.BaseTime);
        var items = Enumerable.Range(0, count)
            .Select(i => BoardItem.CreateRoot(stack.AccountId, stack.WorkspaceId, boardId, group.Id, $"Task {i}", FractionalIndex.Create($"a{i}"), stack.OwnerId, OrgTreeTestHost.BaseTime))
            .ToList();
        await _host.WithSystemContextAsync(c =>
        {
            c.BoardGroups.Add(group);
            c.BoardItems.AddRange(items);
            return Task.CompletedTask;
        });
        return (stack, boardId, items.Select(i => i.Id).ToList());
    }

    private ServiceProvider Provider(OrgStack stack) =>
        _host.CreateProvider(stack.AccountId, stack.OwnerId, services =>
        {
            services.AddScoped<IWorkOrderingScopeLock>(sp => new PostgresWorkOrderingScopeLock(sp.GetRequiredService<ApplicationDbContext>()));
            services.AddScoped<WorkPlacementResolver>();
        });

    private async Task<Guid> SeedSelectFieldAsync(OrgStack stack, Guid boardId)
    {
        var field = BoardField.Create(stack.AccountId, stack.WorkspaceId, boardId, "Risk", FieldType.Select,
            FieldSettings.Empty(), FractionalIndex.Create("a5"), stack.OwnerId, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(c => { c.BoardFields.Add(field); return Task.CompletedTask; });
        return field.Id;
    }

    private Task<BoardItem> ItemAsync(Guid id) => _host.ReadAsync(c => c.BoardItems.AsNoTracking().SingleAsync(i => i.Id == id));

    private async Task<long> VersionAsync(Guid itemId) => (await ItemAsync(itemId)).Version;

    private async Task<Dictionary<Guid, long>> VersionsAsync(IEnumerable<Guid> ids)
    {
        var list = ids.ToList();
        return await _host.ReadAsync(async c => await c.BoardItems.AsNoTracking()
            .Where(i => list.Contains(i.Id)).ToDictionaryAsync(i => i.Id, i => i.Version));
    }

    private Task<long> FieldVersionAsync(Guid fieldId) =>
        _host.ReadAsync(c => c.BoardFields.AsNoTracking().Where(f => f.Id == fieldId).Select(f => f.Version).SingleAsync());

    private Task<List<string>> OptionNamesAsync(Guid fieldId) =>
        _host.ReadAsync(async c => (await c.FieldOptions.AsNoTracking().Where(o => o.FieldId == fieldId).ToListAsync())
            .OrderBy(o => o.Position.Value, StringComparer.Ordinal).Select(o => o.Name).ToList());
}
