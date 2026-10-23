using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Behaviors;
using Notrelix.Application.Common.Data;
using Notrelix.Application.Common.Diagnostics;
using Notrelix.Application.Common.Idempotency;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Common.Requests.Execution;
using Notrelix.Application.Features.Accounts.Abstractions;
using Notrelix.Application.Features.Automation.Abstractions;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Documents.Abstractions;
using Notrelix.Application.Features.Documents.Public.PageAuthorization;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Integrations.Abstractions;
using Notrelix.Application.Features.WorkManagement.Abstractions;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetMyBoardItems;
using Notrelix.Application.Features.Workspaces.Abstractions;
using Notrelix.Application.Features.Workspaces.Navigation.Commands.PinResource;
using Notrelix.Application.Features.Workspaces.Navigation.Commands.RecordRecentResource;
using Notrelix.Application.Features.Workspaces.Navigation.Commands.UnpinResource;
using Notrelix.Application.Features.Workspaces.Navigation.Queries.GetPinnedResources;
using Notrelix.Application.Features.Workspaces.Navigation.Queries.GetRecentResources;
using Notrelix.Application.Features.Workspaces.Navigation.Services;
using Notrelix.Domain.Accounts.Accounts;
using Notrelix.Domain.Accounts.Members;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Identity.Users;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.Workspaces;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Navigation;
using Notrelix.Domain.Workspaces.Rules;
using Notrelix.Domain.Workspaces.Workspaces;
using Notrelix.Infrastructure;
using Notrelix.Infrastructure.Data;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Infrastructure.Data.Rls;
using Notrelix.Infrastructure.Operations.Idempotency;
using Notrelix.Infrastructure.Services;
using Notrelix.Infrastructure.Workspaces.Navigation;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Testing.Application.Fakes;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using DomainBusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// WX-BE-001 / WX-BE-002 / WX-BE-005 on PostgreSQL through the real pipeline
/// (execution context, data session transaction, access control, idempotency)
/// and the real navigation store, resource locator and idempotency store:
/// - a member sees only their own recents and pins;
/// - a Board or Doc of another Workspace is rejected as not found;
/// - recents are trimmed to the 30 newest per member per Workspace;
/// - the 50-pin cap holds, including two concurrent pins at the boundary;
/// - idempotent repeats (same key replay, repeated pin/unpin/open) are no-ops;
/// - My Work pages through PostgreSQL by due date and stays per-user and per-Workspace.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class WorkspaceNavigationIntegrationTests : IAsyncLifetime
{
    private static readonly DateTimeOffset BaseTime = new(2026, 10, 10, 8, 0, 0, TimeSpan.Zero);

    private readonly PostgresTestContainer _db;
    private DateTimeOffset _now = BaseTime;

    public WorkspaceNavigationIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.ConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    // ── Visibility ────────────────────────────────────────────────────────

    [Fact]
    public async Task RecentsAndPins_AreVisibleOnlyToTheMemberWhoCreatedThem()
    {
        var stack = await SeedWorkspaceStackAsync();
        var ownerBoard = await SeedBoardAsync(stack.AccountId, stack.WorkspaceId, stack.OwnerId);
        var memberPage = await SeedPageAsync(stack.AccountId, stack.WorkspaceId, stack.OwnerId);
        using var owner = CreateProvider(stack.AccountId, stack.OwnerId);
        using var member = CreateProvider(stack.AccountId, stack.MemberId);

        await SendWithKeyAsync(owner, new RecordRecentResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, ownerBoard));
        await SendAsync(owner, new PinResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, ownerBoard));
        await SendWithKeyAsync(member, new RecordRecentResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Page, memberPage));
        await SendAsync(member, new PinResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Page, memberPage));

        var ownerRecents = await ListRecentsAsync(owner, stack.WorkspaceId);
        var memberRecents = await ListRecentsAsync(member, stack.WorkspaceId);
        var ownerPins = await ListPinsAsync(owner, stack.WorkspaceId);
        var memberPins = await ListPinsAsync(member, stack.WorkspaceId);

        ownerRecents.Should().ContainSingle().Which.ResourceId.Should().Be(ownerBoard);
        memberRecents.Should().ContainSingle().Which.Should().BeEquivalentTo(
            new RecentResourceDto(NavigationResourceKinds.Page, memberPage, BaseTime));
        ownerPins.Should().ContainSingle().Which.ResourceId.Should().Be(ownerBoard);
        memberPins.Should().ContainSingle().Which.ResourceId.Should().Be(memberPage);

        // Unpinning through one member never touches the other member's pin of the same kind.
        await SendAsync(member, new UnpinResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, ownerBoard));
        (await ListPinsAsync(owner, stack.WorkspaceId)).Should().ContainSingle();
    }

    [Fact]
    public async Task RecentsAndPins_DropReferencesToABoardTheMemberCanNoLongerSee()
    {
        var stack = await SeedWorkspaceStackAsync();
        var board = await SeedBoardAsync(stack.AccountId, stack.WorkspaceId, stack.OwnerId);
        using var member = CreateProvider(stack.AccountId, stack.MemberId);

        await SendWithKeyAsync(member, new RecordRecentResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, board));
        await SendAsync(member, new PinResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, board));
        (await ListRecentsAsync(member, stack.WorkspaceId)).Should().ContainSingle();
        (await ListPinsAsync(member, stack.WorkspaceId)).Should().ContainSingle();

        // The owner makes the board private; the member has no explicit access to it.
        await using (var change = _db.CreateContext(SystemTenant()))
        {
            var stored = await change.Boards.SingleAsync(b => b.Id == board);
            stored.ChangeVisibility(BoardVisibility.Private, stack.OwnerId, BaseTime);
            await change.SaveChangesAsync();
        }

        (await ListRecentsAsync(member, stack.WorkspaceId)).Should().BeEmpty(
            "a stored recent must not reveal a board the caller can no longer see");
        (await ListPinsAsync(member, stack.WorkspaceId)).Should().BeEmpty(
            "a stored pin must not reveal a board the caller can no longer see");
    }

    [Fact]
    public async Task NonMember_IsDeniedBeforeAnyNavigationStateIsTouched()
    {
        var stack = await SeedWorkspaceStackAsync();
        var board = await SeedBoardAsync(stack.AccountId, stack.WorkspaceId, stack.OwnerId);
        var outsider = await SeedUserAsync();
        using var provider = CreateProvider(stack.AccountId, outsider);

        var record = () => SendWithKeyAsync(provider, new RecordRecentResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, board));
        var pin = () => SendAsync(provider, new PinResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, board));
        var list = () => ListRecentsAsync(provider, stack.WorkspaceId);

        await record.Should().ThrowAsync<AppForbidden>();
        await pin.Should().ThrowAsync<AppForbidden>();
        await list.Should().ThrowAsync<AppForbidden>();
        (await CountRowsAsync()).Should().Be((0, 0));
    }

    // ── Cross-workspace rejection ─────────────────────────────────────────

    [Fact]
    public async Task ResourceOfAnotherWorkspace_IsRejectedAsNotFound_AndNothingIsStored()
    {
        var stack = await SeedWorkspaceStackAsync();
        var foreignWorkspace = await SeedForeignWorkspaceAsync(stack.AccountId);
        var foreignBoard = await SeedBoardAsync(stack.AccountId, foreignWorkspace, stack.OwnerId);
        var foreignPage = await SeedPageAsync(stack.AccountId, foreignWorkspace, stack.OwnerId);
        using var provider = CreateProvider(stack.AccountId, stack.OwnerId);

        var recordBoard = () => SendWithKeyAsync(provider, new RecordRecentResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, foreignBoard));
        var recordPage = () => SendWithKeyAsync(provider, new RecordRecentResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Page, foreignPage));
        var pinBoard = () => SendAsync(provider, new PinResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, foreignBoard));
        var pinUnknown = () => SendAsync(provider, new PinResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, Guid.CreateVersion7()));
        var kindMismatch = () => SendAsync(provider, new PinResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Page, foreignBoard));

        await recordBoard.Should().ThrowAsync<NotFoundException>();
        await recordPage.Should().ThrowAsync<NotFoundException>();
        await pinBoard.Should().ThrowAsync<NotFoundException>();
        await pinUnknown.Should().ThrowAsync<NotFoundException>();
        await kindMismatch.Should().ThrowAsync<NotFoundException>("a Board id presented as a Doc does not locate a Doc");
        (await CountRowsAsync()).Should().Be((0, 0), "a rejected reference leaves no navigation state behind");
    }

    // ── Recents retention ─────────────────────────────────────────────────

    [Fact]
    public async Task RecordRecent_KeepsOnlyTheThirtyNewestEntries()
    {
        var stack = await SeedWorkspaceStackAsync();
        var boards = await SeedBoardsAsync(stack.AccountId, stack.WorkspaceId, stack.OwnerId, WorkspaceNavigationRules.RecentRetentionLimit + 2);
        using var provider = CreateProvider(stack.AccountId, stack.OwnerId);

        for (var i = 0; i < boards.Count; i++)
        {
            _now = BaseTime.AddMinutes(i);
            await SendWithKeyAsync(provider, new RecordRecentResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, boards[i]));
        }

        var recents = await ListRecentsAsync(provider, stack.WorkspaceId);
        recents.Should().HaveCount(WorkspaceNavigationRules.RecentRetentionLimit);
        recents.Select(r => r.ResourceId).Should().Equal(
            boards.Skip(2).Reverse(),
            "the list is newest first and the two oldest opens were trimmed");
        (await CountRowsAsync()).Recents.Should().Be(WorkspaceNavigationRules.RecentRetentionLimit, "the trim deletes rows, it does not only hide them");

        // Re-opening the oldest retained entry moves it to the front without growing the list.
        _now = BaseTime.AddHours(2);
        await SendWithKeyAsync(provider, new RecordRecentResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, boards[2]));
        var reordered = await ListRecentsAsync(provider, stack.WorkspaceId);
        reordered.Should().HaveCount(WorkspaceNavigationRules.RecentRetentionLimit);
        reordered[0].ResourceId.Should().Be(boards[2]);
        reordered[0].LastOpenedAt.Should().Be(BaseTime.AddHours(2));
    }

    // ── Pin cap ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Pin_IsCappedAtFiftyPerMemberPerWorkspace()
    {
        var stack = await SeedWorkspaceStackAsync();
        var boards = await SeedBoardsAsync(stack.AccountId, stack.WorkspaceId, stack.OwnerId, WorkspaceNavigationRules.PinLimit + 1);
        using var owner = CreateProvider(stack.AccountId, stack.OwnerId);
        using var member = CreateProvider(stack.AccountId, stack.MemberId);

        for (var i = 0; i < WorkspaceNavigationRules.PinLimit; i++)
        {
            _now = BaseTime.AddSeconds(i);
            await SendAsync(owner, new PinResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, boards[i]));
        }

        var overCap = () => SendAsync(owner, new PinResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, boards[^1]));
        (await overCap.Should().ThrowAsync<DomainBusinessRuleException>())
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Navigation_PinLimitReached);

        var pins = await ListPinsAsync(owner, stack.WorkspaceId);
        pins.Should().HaveCount(WorkspaceNavigationRules.PinLimit);
        pins.Select(p => p.ResourceId).Should().Equal(
            boards.Take(WorkspaceNavigationRules.PinLimit).Reverse(),
            "pins are ordered by pin time, newest first");

        // Re-pinning an existing pin at the cap is still a no-op success, and the cap is per member.
        await SendAsync(owner, new PinResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, boards[0]));
        await SendAsync(member, new PinResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, boards[^1]));
        (await ListPinsAsync(member, stack.WorkspaceId)).Should().ContainSingle();
    }

    [Fact]
    public async Task ConcurrentPinsAtTheCapBoundary_AdmitExactlyOne()
    {
        var stack = await SeedWorkspaceStackAsync();
        var boards = await SeedBoardsAsync(stack.AccountId, stack.WorkspaceId, stack.OwnerId, WorkspaceNavigationRules.PinLimit + 1);
        await using (var seed = _db.CreateContext(SystemTenant()))
        {
            for (var i = 0; i < WorkspaceNavigationRules.PinLimit - 1; i++)
            {
                seed.PinnedResources.Add(PinnedResource.Create(
                    stack.AccountId, stack.WorkspaceId, stack.OwnerId, NavigationResourceKinds.Board, boards[i], i, BaseTime.AddSeconds(i)));
            }

            await seed.SaveChangesAsync();
        }

        using var first = CreateProvider(stack.AccountId, stack.OwnerId);
        using var second = CreateProvider(stack.AccountId, stack.OwnerId);

        var outcomes = await Task.WhenAll(
            CaptureAsync(() => SendAsync(first, new PinResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, boards[^2]))),
            CaptureAsync(() => SendAsync(second, new PinResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, boards[^1]))));

        outcomes.Count(o => o is null).Should().Be(1, "the member navigation lock serializes the two pins");
        outcomes.Single(o => o is not null).Should().BeOfType<DomainBusinessRuleException>()
            .Which.RuleCode.Should().Be(WorkspaceRuleCodes.Workspaces_Navigation_PinLimitReached);
        (await CountRowsAsync()).Pins.Should().Be(WorkspaceNavigationRules.PinLimit);
    }

    [Fact]
    public async Task ConcurrentFirstOpensOfTheSameResource_StoreOneEntry()
    {
        var stack = await SeedWorkspaceStackAsync();
        var board = await SeedBoardAsync(stack.AccountId, stack.WorkspaceId, stack.OwnerId);
        using var first = CreateProvider(stack.AccountId, stack.OwnerId);
        using var second = CreateProvider(stack.AccountId, stack.OwnerId);

        var outcomes = await Task.WhenAll(
            CaptureAsync(() => SendWithKeyAsync(first, new RecordRecentResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, board))),
            CaptureAsync(() => SendWithKeyAsync(second, new RecordRecentResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, board))));

        outcomes.Should().AllSatisfy(o => o.Should().BeNull("two tabs opening the same Board are both accepted"));
        (await CountRowsAsync()).Recents.Should().Be(1, "the second open updates the entry instead of violating the unique key");
    }

    // ── Idempotent repeats ────────────────────────────────────────────────

    [Fact]
    public async Task IdempotentRepeats_AreNoOps()
    {
        var stack = await SeedWorkspaceStackAsync();
        var board = await SeedBoardAsync(stack.AccountId, stack.WorkspaceId, stack.OwnerId);
        using var provider = CreateProvider(stack.AccountId, stack.OwnerId);
        var key = $"wx-recent-{Guid.NewGuid():N}";

        // Same Idempotency-Key + same request: one execution, the retry replays.
        await SendWithKeyAsync(provider, new RecordRecentResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, board), key);
        _now = BaseTime.AddMinutes(10);
        var replayed = await SendWithKeyAsync(provider, new RecordRecentResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, board), key);
        replayed.Should().BeTrue("the retried request is served from the idempotency record");
        (await ListRecentsAsync(provider, stack.WorkspaceId)).Should().ContainSingle()
            .Which.LastOpenedAt.Should().Be(BaseTime, "a replay does not execute the open again");

        // A new open (new key) of the same resource upserts, never duplicates.
        await SendWithKeyAsync(provider, new RecordRecentResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, board));
        (await ListRecentsAsync(provider, stack.WorkspaceId)).Should().ContainSingle()
            .Which.LastOpenedAt.Should().Be(BaseTime.AddMinutes(10));

        // PUT pin twice: one pin, original pin time kept.
        _now = BaseTime.AddMinutes(20);
        await SendAsync(provider, new PinResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, board));
        _now = BaseTime.AddMinutes(30);
        await SendAsync(provider, new PinResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, board));
        (await ListPinsAsync(provider, stack.WorkspaceId)).Should().ContainSingle()
            .Which.PinnedAt.Should().Be(BaseTime.AddMinutes(20));

        // DELETE pin twice: both succeed, nothing left.
        var unpin = new UnpinResourceCommand(stack.WorkspaceId, NavigationResourceKinds.Board, board);
        ((Result)(await SendAsync(provider, unpin))!).Succeeded.Should().BeTrue();
        ((Result)(await SendAsync(provider, unpin))!).Succeeded.Should().BeTrue();
        (await CountRowsAsync()).Should().Be((1, 0));
    }

    // ── My Work (WX-BE-005) ───────────────────────────────────────────────

    [Fact]
    public async Task MyWork_PagesByDueDate_AndReturnsOnlyTheCallersOpenItemsInTheWorkspace()
    {
        var stack = await SeedWorkspaceStackAsync();
        var foreignWorkspace = await SeedForeignWorkspaceAsync(stack.AccountId);
        var now = BaseTime;
        var board = Board.Create(stack.AccountId, stack.WorkspaceId, stack.OwnerId, "My Work Board", null, now);
        var group = BoardGroup.Create(stack.AccountId, stack.WorkspaceId, board.Id, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), stack.OwnerId, now);
        var foreignBoard = Board.Create(stack.AccountId, foreignWorkspace, stack.OwnerId, "Foreign Board", null, now);
        var foreignGroup = BoardGroup.Create(stack.AccountId, foreignWorkspace, foreignBoard.Id, "Todo", Color.Create("#808080"), FractionalIndex.Create("a0"), stack.OwnerId, now);

        var position = 0;
        BoardItem Item(string name, int? dueInDays, Guid? workspaceId = null, Board? onBoard = null, BoardGroup? inGroup = null)
        {
            var b = onBoard ?? board;
            var g = inGroup ?? group;
            var item = BoardItem.CreateRoot(stack.AccountId, workspaceId ?? stack.WorkspaceId, b.Id, g.Id, name, FractionalIndex.Create($"a{position++}"), stack.OwnerId, now);
            if (dueInDays is { } days)
                item.SetTimeline(null, now.AddDays(days), stack.OwnerId, now);
            return item;
        }

        var overdue = Item("overdue", -2);
        var today = Item("today", 0);
        var thisWeek = Item("this week", 3);
        var undated = Item("undated", null);
        var completed = Item("completed", 1);
        completed.Complete(now, stack.OwnerId, now);
        var someoneElses = Item("member's", 1);
        var foreign = Item("foreign", 1, foreignWorkspace, foreignBoard, foreignGroup);

        await using (var seed = _db.CreateContext(SystemTenant()))
        {
            seed.Boards.AddRange(board, foreignBoard);
            seed.BoardGroups.AddRange(group, foreignGroup);
            seed.BoardItems.AddRange(overdue, today, thisWeek, undated, completed, someoneElses, foreign);
            foreach (var mine in new[] { overdue, today, thisWeek, undated, completed })
                seed.BoardItemMembers.Add(BoardItemMember.Create(stack.AccountId, stack.WorkspaceId, board.Id, mine.Id, stack.OwnerId, stack.OwnerId, now));
            seed.BoardItemMembers.Add(BoardItemMember.Create(stack.AccountId, stack.WorkspaceId, board.Id, someoneElses.Id, stack.MemberId, stack.OwnerId, now));
            seed.BoardItemMembers.Add(BoardItemMember.Create(stack.AccountId, foreignWorkspace, foreignBoard.Id, foreign.Id, stack.OwnerId, stack.OwnerId, now));
            await seed.SaveChangesAsync();
        }

        using var provider = CreateProvider(stack.AccountId, stack.OwnerId);

        var seen = new List<MyWorkItemDto>();
        string? cursor = null;
        var pages = 0;
        do
        {
            var page = ((Result<CursorPage<MyWorkItemDto>>)(await SendAsync(provider, new GetMyBoardItemsQuery(stack.WorkspaceId, cursor, 2)))!).Data!;
            page.Items.Length.Should().BeLessThanOrEqualTo(2);
            seen.AddRange(page.Items);
            cursor = page.NextCursor;
            pages++;
        }
        while (cursor is not null && pages < 10);

        pages.Should().Be(2);
        seen.Select(i => i.Id).Should().Equal(
            new[] { overdue.Id, today.Id, thisWeek.Id, undated.Id },
            "PostgreSQL keyset pages are ordered by due date, undated last, and exclude completed, other users' and foreign items");
        seen.Should().AllSatisfy(i =>
        {
            i.BoardId.Should().Be(board.Id);
            i.GroupId.Should().Be(group.Id);
        });
        seen[0].DueAt.Should().Be(now.AddDays(-2));
        seen[3].DueAt.Should().BeNull();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static async Task<Exception?> CaptureAsync(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private static async Task<object?> SendAsync(ServiceProvider provider, object request)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    /// <summary>Sends with an Idempotency-Key bound as the HTTP filter does; returns whether it replayed.</summary>
    private static async Task<bool> SendWithKeyAsync(ServiceProvider provider, RecordRecentResourceCommand command, string? key = null)
    {
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IIdempotencyExecutionContextWriter>()
            .Set(key ?? $"wx-recent-{Guid.NewGuid():N}", IdempotencyExecutionSource.Internal);
        var result = (Result)(await scope.ServiceProvider.GetRequiredService<ISender>().Send(command))!;
        result.Succeeded.Should().BeTrue();
        return scope.ServiceProvider.GetRequiredService<IIdempotencyExecutionContext>().IsReplay;
    }

    private static async Task<IReadOnlyList<RecentResourceDto>> ListRecentsAsync(ServiceProvider provider, Guid workspaceId) =>
        ((Result<IReadOnlyList<RecentResourceDto>>)(await SendAsync(provider, new GetRecentResourcesQuery(workspaceId)))!).Data!;

    private static async Task<IReadOnlyList<PinnedResourceDto>> ListPinsAsync(ServiceProvider provider, Guid workspaceId) =>
        ((Result<IReadOnlyList<PinnedResourceDto>>)(await SendAsync(provider, new GetPinnedResourcesQuery(workspaceId)))!).Data!;

    private async Task<(int Recents, int Pins)> CountRowsAsync()
    {
        await using var verify = _db.CreateContext(SystemTenant());
        return (await verify.RecentResources.CountAsync(), await verify.PinnedResources.CountAsync());
    }

    private async Task<Guid> SeedBoardAsync(Guid accountId, Guid workspaceId, Guid ownerId) =>
        (await SeedBoardsAsync(accountId, workspaceId, ownerId, 1))[0];

    private async Task<List<Guid>> SeedBoardsAsync(Guid accountId, Guid workspaceId, Guid ownerId, int count)
    {
        var boards = Enumerable.Range(0, count)
            .Select(i => Board.Create(accountId, workspaceId, ownerId, $"Board {i}", null, BaseTime))
            .ToList();
        await using var seed = _db.CreateContext(SystemTenant());
        seed.Boards.AddRange(boards);
        await seed.SaveChangesAsync();
        return boards.Select(b => b.Id).ToList();
    }

    private async Task<Guid> SeedPageAsync(Guid accountId, Guid workspaceId, Guid ownerId)
    {
        var page = Page.Create(accountId, workspaceId, "Navigation Doc", ownerId, BaseTime);
        await using var seed = _db.CreateContext(SystemTenant());
        seed.Pages.Add(page);
        await seed.SaveChangesAsync();
        return page.Id;
    }

    private async Task<Guid> SeedForeignWorkspaceAsync(Guid accountId)
    {
        var foreign = Workspace.Create(accountId, Guid.NewGuid(), "Foreign WS", $"wx-f-{Guid.NewGuid():N}", BaseTime);
        await using var seed = _db.CreateContext(SystemTenant());
        seed.Workspaces.Add(foreign);
        await seed.SaveChangesAsync();
        return foreign.Id;
    }

    private async Task<Guid> SeedUserAsync()
    {
        var user = User.Create($"wx-{Guid.NewGuid():N}@example.com", "WX Outsider", "hashed", BaseTime, true);
        user.ConfirmEmail(user.Id, BaseTime);
        await using var seed = _db.CreateContext(SystemTenant());
        seed.Users.Add(user);
        await seed.SaveChangesAsync();
        return user.Id;
    }

    private async Task<(Guid AccountId, Guid OwnerId, Guid MemberId, Guid WorkspaceId)> SeedWorkspaceStackAsync()
    {
        var now = BaseTime;
        var owner = User.Create($"wx-{Guid.NewGuid():N}@example.com", "WX Owner", "hashed", now, true);
        owner.ConfirmEmail(owner.Id, now);
        var member = User.Create($"wx-{Guid.NewGuid():N}@example.com", "WX Member", "hashed", now, true);
        member.ConfirmEmail(member.Id, now);
        var account = Account.Create("WX Account", $"wx-{Guid.NewGuid():N}", AccountType.Team, owner.Id, now);
        var workspace = Workspace.Create(account.Id, owner.Id, "WX Workspace", $"wx-ws-{Guid.NewGuid():N}", now);

        await using (var seed = _db.CreateContext(SystemTenant()))
        {
            seed.Users.Add(owner);
            seed.Users.Add(member);
            seed.Accounts.Add(account);
            seed.AccountMembers.Add(AccountMember.Create(account.Id, owner.Id, AccountRole.Owner, owner.Id, now));
            seed.AccountMembers.Add(AccountMember.Create(account.Id, member.Id, AccountRole.Member, owner.Id, now));
            seed.Workspaces.Add(workspace);
            seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, owner.Id, WorkspaceRole.Owner, owner.Id, now));
            seed.WorkspaceMembers.Add(WorkspaceMember.Create(account.Id, workspace.Id, member.Id, WorkspaceRole.Member, owner.Id, now));
            await seed.SaveChangesAsync();
        }

        await using var grant = _db.CreateContext(SystemTenant());
        var projection = new AccessGrantProjectionService(grant);
        await projection.SyncWorkspaceMemberGrantAsync(account.Id, workspace.Id, owner.Id, WorkspaceRole.Owner, now, CancellationToken.None);
        await projection.SyncWorkspaceMemberGrantAsync(account.Id, workspace.Id, member.Id, WorkspaceRole.Member, now, CancellationToken.None);
        await grant.SaveChangesAsync();

        return (account.Id, owner.Id, member.Id, workspace.Id);
    }

    private static FakeCurrentTenantContext SystemTenant()
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetSystem();
        return tenant;
    }

    /// <summary>
    /// The production pipeline slice used by the Work integration suites, with the
    /// navigation handlers, the real navigation store and the real resource locator.
    /// </summary>
    private ServiceProvider CreateProvider(Guid accountId, Guid userId)
    {
        var tenant = new FakeCurrentTenantContext();
        tenant.SetAccount(accountId, userId);

        var requestContextMock = new Mock<ICurrentRequestContext>();
        requestContextMock.Setup(r => r.UserId).Returns(userId);
        requestContextMock.Setup(r => r.RequireAccountId()).Returns(accountId);
        requestContextMock.Setup(r => r.RequireWorkspaceId()).Returns(() => tenant.WorkspaceId ?? Guid.Empty);

        var currentUserMock = new Mock<ICurrentUser>();
        currentUserMock.Setup(u => u.UserId).Returns(userId);
        currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);

        var credentialMock = new Mock<ICurrentCredentialContext>();
        credentialMock.Setup(c => c.Kind).Returns(CredentialKind.UserSession);

        var clockMock = new Mock<IDateTimeProvider>();
        clockMock.Setup(c => c.UtcNow).Returns(() => _now);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();

        services.AddSingleton(new MediatRServiceConfiguration());
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<ISender>(sp => sp.GetRequiredService<IMediator>());

        services.AddSingleton(requestContextMock.Object);
        services.AddSingleton(currentUserMock.Object);
        services.AddSingleton<ICurrentTenantContext>(tenant);
        services.AddSingleton(credentialMock.Object);
        services.AddSingleton(clockMock.Object);
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IRequestDescriptorRegistry>(
            RequestDescriptorRegistry.Create(typeof(RecordRecentResourceCommand).Assembly));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionMappingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(RequestContractBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExecutionContextBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DataSessionBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AccessControlBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));

        services.AddScoped<ApplicationDbContext>(sp =>
            _db.CreateContext(sp.GetRequiredService<ICurrentTenantContext>()));
        services.AddScoped<IWorkspaceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAccountDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IGovernanceDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IWorkManagementDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IDocumentDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<ICollaborationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IAutomationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IIntegrationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddSingleton<IOptions<RlsOptions>>(Options.Create(new RlsOptions
        {
            Enabled = true,
            SetSessionContext = true,
        }));
        services.AddScoped<IRlsSessionContext, RlsSessionContext>();
        services.AddScoped<IRequestDataSession, EfRequestDataSession>();

        services.AddSingleton<PipelineMetrics>();
        services.AddSingleton<IAccessPolicyEvaluator, AccessPolicyEngine>();
        services.AddScoped<IPageAuthorizationFacts, PostgresPageAuthorizationFacts>();
        services.AddScoped<IAccessFactsProvider>(sp =>
            new PostgresAccessFactsProvider(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<IPageAuthorizationFacts>(),
                new FakeBillingSubscriptionFacts()));
        services.AddGovernanceInfrastructure(new ConfigurationBuilder().Build());

        services.AddScoped<IResourceLocator, ResourceLocator>();
        services.AddScoped<global::Notrelix.Application.Common.Tenancy.ITenantBootstrapStore, TenantBootstrapStore>();

        services.AddScoped<Notrelix.Application.Common.Context.ExecutionContext>();
        services.AddScoped<IExecutionContextAccessor>(sp =>
            sp.GetRequiredService<Notrelix.Application.Common.Context.ExecutionContext>());
        services.AddScoped<IExecutionContextReader>(sp =>
            sp.GetRequiredService<Notrelix.Application.Common.Context.ExecutionContext>());

        services.AddOptions<IdempotencyOptions>().Configure(_ => { });
        services.AddSingleton<IIdempotencyRequestFingerprint, JsonIdempotencyRequestFingerprint>();
        services.AddSingleton<IIdempotencyReplayPolicy, DefaultIdempotencyReplayPolicy>();
        services.AddScoped<IdempotencyPartitionFactory>();
        services.AddScoped<IIdempotencyStore>(sp =>
            new EfIdempotencyStore(
                sp.GetRequiredService<ApplicationDbContext>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<IOptions<IdempotencyOptions>>()));
        services.AddScoped<IdempotencyExecutionContext>();
        services.AddScoped<IIdempotencyExecutionContext>(sp =>
            sp.GetRequiredService<IdempotencyExecutionContext>());
        services.AddScoped<IIdempotencyExecutionContextWriter>(sp =>
            sp.GetRequiredService<IdempotencyExecutionContext>());

        services.AddScoped<IWorkspaceNavigationStore>(sp => new WorkspaceNavigationStore(sp.GetRequiredService<ApplicationDbContext>()));
        services.AddScoped<global::Notrelix.Application.Features.Governance.Public.Visibility.IResourceVisibility>(sp =>
            new PostgresResourceVisibility(sp.GetRequiredService<ApplicationDbContext>(), sp.GetRequiredService<TimeProvider>()));
        services.AddScoped<IRequestHandler<RecordRecentResourceCommand, Result>, RecordRecentResourceCommandHandler>();
        services.AddScoped<IRequestHandler<PinResourceCommand, Result>, PinResourceCommandHandler>();
        services.AddScoped<IRequestHandler<UnpinResourceCommand, Result>, UnpinResourceCommandHandler>();
        services.AddScoped<IRequestHandler<GetRecentResourcesQuery, Result<IReadOnlyList<RecentResourceDto>>>, GetRecentResourcesQueryHandler>();
        services.AddScoped<IRequestHandler<GetPinnedResourcesQuery, Result<IReadOnlyList<PinnedResourceDto>>>, GetPinnedResourcesQueryHandler>();
        services.AddScoped<IRequestHandler<GetMyBoardItemsQuery, Result<CursorPage<MyWorkItemDto>>>, GetMyBoardItemsQueryHandler>();

        return services.BuildServiceProvider();
    }
}
