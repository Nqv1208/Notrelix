using Microsoft.Extensions.DependencyInjection;
using Notrelix.Application.Features.Governance.Abstractions;
using Notrelix.Application.Features.Governance.Public.Access;
using Notrelix.Application.Features.WorkManagement.Public.BoardAccess;
using Notrelix.Application.Features.Workspaces.Spaces.Queries.GetSpaceOverview;
using Notrelix.Domain.Documents.Pages;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Domain.SharedKernel;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Domain.WorkManagement.Approvals;
using Notrelix.Domain.WorkManagement.Fields;
using Notrelix.Domain.WorkManagement.BoardGroups;
using Notrelix.Domain.WorkManagement.Boards;
using Notrelix.Domain.WorkManagement.Items;
using Notrelix.Domain.WorkManagement.Views;
using Notrelix.Domain.Workspaces.Members;
using Notrelix.Domain.Workspaces.Spaces;
using Notrelix.Domain.Workspaces.Teams;
using Notrelix.Infrastructure.CrossContext.Governance.WorkManagement;
using Notrelix.Infrastructure.Data.Authz;
using Notrelix.Integration.Tests.Containers;
using Notrelix.Integration.Tests.Workspaces.OrgTree;
using AppForbidden = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFound = Notrelix.Application.Common.Exceptions.NotFoundException;

namespace Notrelix.Integration.Tests.Workspaces;

/// <summary>
/// CTR-GAP-WS-SPACE-OVERVIEW on PostgreSQL through the real pipeline (NRX-003, NRX-004, GOV-009, GOV-025,
/// GOV-029): the running-board rule, Board and Page figures only from the caller's readable ids, child Space
/// counts only for Spaces the caller can read, the member preview, and refusals for a Private Space the
/// caller cannot see, a Guest, an outsider and another Workspace.
/// </summary>
[Collection("Database")]
[Trait("Category", "Integration")]
public sealed class SpaceOverviewIntegrationTests : IAsyncLifetime
{
    private readonly PostgresTestContainer _db;
    private readonly OrgTreeTestHost _host;

    public SpaceOverviewIntegrationTests(PostgresTestContainer db)
    {
        _db = db;
        _host = new OrgTreeTestHost(db);
    }

    public async Task InitializeAsync() => await new DatabaseReset(_db.OwnerConnectionString).ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task RunningBoards_AreUnarchivedReadableWithAnUnfinishedItem_FinishedAndEmptyOnesOnlyCountInBoardTotal()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var space = await SeedSpaceAsync(stack, member);
        var running = await SeedBoardWithItemsAsync(stack, "Running", space, member, ("done", true, null, true), ("late", false, -2, true), ("open", false, 5, true));
        var finished = await SeedBoardWithItemsAsync(stack, "Finished", space, member, ("done", true, null, true));
        var empty = await _host.SeedBoardAsync(stack, "Empty", spaceId: space);
        var archived = await _host.SeedBoardAsync(stack, "Archived", spaceId: space, transition: b => b.Archive(stack.OwnerId, _host.Now));
        await SeedBoardWithItemsAsync(stack, "Elsewhere", null, member, ("open", false, null, true));

        var overview = await GetAsync(stack, member, space);

        overview.BoardTotal.Should().Be(3, "Running, Finished and Empty are readable and not archived; Archived and the unplaced Board are not");
        var board = overview.Boards.Should().ContainSingle().Subject;
        board.Id.Should().Be(running);
        board.Kind.Should().Be("table");
        board.IsPrivate.Should().BeFalse();
        board.AddedCount.Should().BeNull();
        board.Progress.Should().Be(new SpaceOverviewProgressDto(1, 3));
        board.Attention.Should().BeEquivalentTo(new SpaceOverviewBoardAttentionDto(1, 0, 0));
        board.People.Select(p => p.Id).Should().Equal(member);
        overview.Boards.Select(b => b.Id).Should().NotContain([finished, empty, archived]);
        overview.Attention.Should().ContainSingle(a => a.Id == "overdue" && a.Count == 1 && a.BoardId == running);
        overview.Workload.Should().ContainSingle().Which.Should().Match<SpaceOverviewWorkloadDto>(w => w.Person.Id == member && w.Active == 2 && w.Overdue == 1);
    }

    [Fact]
    public async Task PrivateBoardTheCallerCannotRead_IsHiddenAndNotCounted_AMemberOfItSeesItWithAddedCount()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var insider = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Insider");
        var space = await SeedSpaceAsync(stack, member, insider);
        var open = await SeedBoardWithItemsAsync(stack, "Open", space, member, ("a", false, null, true));
        var secret = await SeedBoardWithItemsAsync(stack, "Secret", space, insider, ("b", false, null, true), visibility: BoardVisibility.Private);
        await _host.SeedBoardMemberAsync(secret, insider, BoardRole.Owner);

        var asMember = await GetAsync(stack, member, space);
        var asInsider = await GetAsync(stack, insider, space);

        asMember.BoardTotal.Should().Be(1);
        asMember.Boards.Select(b => b.Id).Should().Equal(open);
        asMember.Workload.Should().OnlyContain(w => w.Person.Id == member, "workload counts only Boards the caller can read");
        asInsider.BoardTotal.Should().Be(2);
        var secretDto = asInsider.Boards.Single(b => b.Id == secret);
        secretDto.IsPrivate.Should().BeTrue();
        secretDto.AddedCount.Should().Be(1);
    }

    [Fact]
    public async Task ChildSpaces_AreCountedOnlyWhenTheCallerCanReadThem_AndTheMemberPreviewDropsPrivateBoardsAndChildren()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var space = await SeedSpaceAsync(stack, member);
        var visibleChild = await _host.SeedSpaceAsync(stack, "Visible child", SpaceVisibility.Workspace, stack.OwnerId, parentId: space, type: SpaceType.Department, transition: s =>
            s.AddMember(SpaceMemberSubjectType.User, member, SpaceRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime));
        var hiddenChild = await _host.SeedSpaceAsync(stack, "Hidden child", SpaceVisibility.Private, stack.OwnerId, parentId: space, type: SpaceType.Department);
        await SeedBoardWithItemsAsync(stack, "Child board", visibleChild, member, ("a", false, null, true));
        var secret = await SeedBoardWithItemsAsync(stack, "Secret", space, stack.OwnerId, ("b", false, null, true), visibility: BoardVisibility.Private);
        await _host.SeedBoardMemberAsync(secret, stack.OwnerId, BoardRole.Owner);

        var asMember = await GetAsync(stack, member, space);
        var asOwner = await GetAsync(stack, stack.OwnerId, space);
        var preview = await GetAsync(stack, stack.OwnerId, space, asMember: true);

        asMember.Children.Should().ContainSingle().Which.Should().Be(new SpaceOverviewChildDto(visibleChild, 2, 1));
        asOwner.Children.Select(c => c.Id).Should().BeEquivalentTo([visibleChild, hiddenChild]);
        asOwner.Boards.Select(b => b.Id).Should().Contain(secret);
        preview.Children.Select(c => c.Id).Should().Equal(visibleChild);
        preview.Boards.Select(b => b.Id).Should().NotContain(secret);
        preview.BoardTotal.Should().Be(0);
    }

    [Fact]
    public async Task PeopleAndAccess_CountEachPersonOnceAtTheHighestRole_WithTeamsExpanded()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var linh = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Linh");
        var hoa = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Hoa");
        var minh = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Minh");
        var team = Team.Create(stack.AccountId, stack.WorkspaceId, "Accounting", stack.OwnerId, OrgTreeTestHost.BaseTime);
        team.AddMember(linh, TeamMemberRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
        team.AddMember(hoa, TeamMemberRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
        await _host.WithSystemContextAsync(c => { c.Teams.Add(team); return Task.CompletedTask; });
        var space = await _host.SeedSpaceAsync(stack, "Finance", SpaceVisibility.Workspace, stack.OwnerId, type: SpaceType.Department, transition: s =>
        {
            s.AddMember(SpaceMemberSubjectType.Team, team.Id, SpaceRole.Viewer, stack.OwnerId, OrgTreeTestHost.BaseTime);
            s.AddMember(SpaceMemberSubjectType.User, hoa, SpaceRole.Admin, stack.OwnerId, OrgTreeTestHost.BaseTime);
            s.AddMember(SpaceMemberSubjectType.User, minh, SpaceRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
        });

        var overview = await GetAsync(stack, minh, space);

        overview.PeopleTotal.Should().Be(4);
        overview.People.Should().HaveCount(4);
        overview.People.Take(2).Select(p => p.Id).Should().BeEquivalentTo([stack.OwnerId, hoa], "admins first");
        overview.Access.Admins.Select(a => a.Id).Should().BeEquivalentTo([stack.OwnerId, hoa]);
        overview.Access.Members.People.Should().Be(1);
        overview.Access.Viewers.Should().BeEquivalentTo(new SpaceOverviewRoleDto(1, [new SpaceOverviewTeamDto("Accounting", 2)]));
    }

    [Fact]
    public async Task Docs_CountOnlyReadableRootPagesOfTheSpace_NewestFirst()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var space = await SeedSpaceAsync(stack, member);
        var otherSpace = await SeedSpaceAsync(stack, member);
        Guid rootId = Guid.Empty;
        await _host.WithSystemContextAsync(c =>
        {
            var root = Page.Create(stack.AccountId, stack.WorkspaceId, "Root", stack.OwnerId, OrgTreeTestHost.BaseTime, spaceId: space);
            var nested = Page.Create(stack.AccountId, stack.WorkspaceId, "Nested", stack.OwnerId, OrgTreeTestHost.BaseTime, parentId: root.Id, spaceId: space);
            var elsewhere = Page.Create(stack.AccountId, stack.WorkspaceId, "Elsewhere", stack.OwnerId, OrgTreeTestHost.BaseTime, spaceId: otherSpace);
            c.Pages.AddRange(root, nested, elsewhere);
            rootId = root.Id;
            return Task.CompletedTask;
        });

        var overview = await GetAsync(stack, stack.OwnerId, space);

        overview.DocTotal.Should().Be(1);
        var doc = overview.Docs.Should().ContainSingle().Subject;
        (doc.Id, doc.Title).Should().Be((rootId, "Root"));
    }

    [Fact]
    public async Task Guests_AreListedWithTheirBoards_OnlyToACallerWhoSeesTheBoardEntries_AndNotInThePreview()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var space = await SeedSpaceAsync(stack, member);
        var board = await SeedBoardWithItemsAsync(stack, "Shared", space, member, ("a", false, null, true));
        await _host.SeedResourcePermissionAsync(stack, "work-management.board", board, PermissionSubjectType.User, guest, PermissionLevel.Editor);

        var asOwner = await GetAsync(stack, stack.OwnerId, space);
        var asPlainMember = await GetAsync(stack, member, space);
        var preview = await GetAsync(stack, stack.OwnerId, space, asMember: true);

        var seen = asOwner.Access.Guests.Should().ContainSingle().Subject;
        seen.Id.Should().Be(guest);
        seen.Boards.Select(b => b.Id).Should().Equal(board);
        asPlainMember.Access.Guests.Should().BeEmpty("a member who is not on the Board and does not manage it gets counts only");
        preview.Access.Guests.Should().BeEmpty();
    }

    [Fact]
    public async Task APrivateSpaceTheCallerCannotSee_IsNotFound_GuestOutsiderAndOtherWorkspaceAreRefused_PreviewNeedsAManager()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var other = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var guest = await _host.AddMemberAsync(stack, WorkspaceRole.Guest, "Guest");
        var outsider = await _host.AddOutsiderAsync(stack);
        var hidden = await _host.SeedSpaceAsync(stack, "Board of directors", SpaceVisibility.Private, stack.OwnerId);
        var visible = await SeedSpaceAsync(stack, member);
        var foreign = await _host.SeedSpaceAsync(other, "Foreign", SpaceVisibility.Workspace, other.OwnerId);

        var hiddenRead = () => GetAsync(stack, member, hidden);
        await hiddenRead.Should().ThrowAsync<AppNotFound>("a Private Space the member cannot see answers like a missing one");
        (await GetAsync(stack, stack.OwnerId, hidden)).PeopleTotal.Should().Be(1);

        foreach (var userId in new[] { guest, outsider })
        {
            var read = () => GetAsync(stack, userId, visible);
            await read.Should().ThrowAsync<AppForbidden>();
        }

        var cross = () => GetAsync(stack, stack.OwnerId, foreign);
        await cross.Should().ThrowAsync<AppNotFound>("a Space of another Workspace is unreachable");

        var hiddenPreview = () => GetAsync(stack, member, hidden, asMember: true);
        await hiddenPreview.Should().ThrowAsync<AppNotFound>("asking for the preview must not reveal a Space the caller cannot see");

        var preview = () => GetAsync(stack, member, visible, asMember: true);
        await preview.Should().ThrowAsync<AppForbidden>("only a department manager may preview the department as a member");
    }

    [Fact]
    public async Task InboxBoard_ReportsUnclaimedItems_AndWaitingReviewCountsUnfinishedItemsWithAPendingApprovalOnceEach()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var space = await SeedSpaceAsync(stack, member);
        var inbox = await SeedBoardWithItemsAsync(stack, "Requests", space, member, ("a", false, null, false), ("b", false, null, true), ("c", false, null, true), ("d", true, null, false));
        await _host.WithSystemContextAsync(async c =>
        {
            var board = await c.Boards.SingleAsync(b => b.Id == inbox);
            typeof(Board).GetProperty(nameof(Board.BoardFamily))!.SetValue(board, BoardFamily.Intake);
            var items = await c.BoardItems.Where(i => i.BoardId == inbox).ToListAsync();
            Guid IdOf(string name) => items.Single(i => i.Name == name).Id;
            ApprovalRequest On(Guid itemId, string title) => ApprovalRequest.Create(
                stack.AccountId, stack.WorkspaceId, ResourceRef.Create(ResourceKind.Create("work-management.board-item"), itemId, stack.WorkspaceId), title, stack.OwnerId, _host.Now);

            // Item a has two pending requests but is one waiting Item; b has one; c has none; d is finished.
            c.ApprovalRequests.Add(On(IdOf("a"), "Review 1"));
            c.ApprovalRequests.Add(On(IdOf("a"), "Review 2"));
            c.ApprovalRequests.Add(On(IdOf("b"), "Review"));
            c.ApprovalRequests.Add(On(IdOf("d"), "Review of a finished item"));
            // A request aimed at the Board itself is not an Item.
            c.ApprovalRequests.Add(ApprovalRequest.Create(
                stack.AccountId, stack.WorkspaceId, ResourceRef.Create(ResourceKind.Create("work-management.board"), inbox, stack.WorkspaceId), "Board review", stack.OwnerId, _host.Now));
        });

        var overview = await GetAsync(stack, member, space);

        var board = overview.Boards.Should().ContainSingle().Subject;
        board.Kind.Should().Be("inbox");
        board.Attention.Should().BeEquivalentTo(new SpaceOverviewBoardAttentionDto(0, 2, 1));
        overview.Attention.Select(a => (a.Id, a.Count)).Should().BeEquivalentTo([("unassigned", 1), ("review", 2)]);
    }

    [Fact]
    public async Task BoardKind_IntakeIsInbox_OnlyADefaultKanbanViewIsKanban_EverythingElseIsATable()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var member = await _host.AddMemberAsync(stack, WorkspaceRole.Member, "Member");
        var space = await SeedSpaceAsync(stack, member);
        var kanban = await SeedBoardWithItemsAsync(stack, "Kanban default", space, member, ("a", false, null, true));
        var tableDefault = await SeedBoardWithItemsAsync(stack, "Table default", space, member, ("a", false, null, true));
        var noDefault = await SeedBoardWithItemsAsync(stack, "No default", space, member, ("a", false, null, true));
        var noView = await SeedBoardWithItemsAsync(stack, "No view", space, member, ("a", false, null, true));
        var intake = await SeedBoardWithItemsAsync(stack, "Intake with kanban", space, member, ("a", false, null, true));
        await SeedViewAsync(stack, kanban, ViewType.Kanban, isDefault: true);
        await SeedViewAsync(stack, tableDefault, ViewType.Table, isDefault: true);
        await SeedViewAsync(stack, tableDefault, ViewType.Kanban, isDefault: false);
        await SeedViewAsync(stack, noDefault, ViewType.Kanban, isDefault: false);
        await SeedViewAsync(stack, intake, ViewType.Kanban, isDefault: true);
        await _host.WithSystemContextAsync(async c =>
        {
            var board = await c.Boards.SingleAsync(b => b.Id == intake);
            typeof(Board).GetProperty(nameof(Board.BoardFamily))!.SetValue(board, BoardFamily.Intake);
        });

        var overview = await GetAsync(stack, member, space);

        overview.Boards.ToDictionary(b => b.Id, b => b.Kind).Should().BeEquivalentTo(new Dictionary<Guid, string>
        {
            [kanban] = "kanban",
            [tableDefault] = "table",
            [noDefault] = "table",
            [noView] = "table",
            [intake] = "inbox",
        });
    }

    [Fact]
    public async Task Caps_AreSixBoards_FourDocs_FiveWorkloadPeople_WhileTotalsKeepCounting()
    {
        var stack = await _host.SeedWorkspaceAsync();
        var members = new List<Guid>();
        for (var i = 0; i < 7; i++)
            members.Add(await _host.AddMemberAsync(stack, WorkspaceRole.Member, $"Member {i}"));
        var space = await SeedSpaceAsync(stack, members.ToArray());
        for (var i = 0; i < 8; i++)
            await SeedBoardWithItemsAsync(stack, $"Board {i}", space, members[i % members.Count], ("open", false, null, true));
        await _host.WithSystemContextAsync(c =>
        {
            for (var i = 0; i < 6; i++)
                c.Pages.Add(Page.Create(stack.AccountId, stack.WorkspaceId, $"Doc {i}", stack.OwnerId, OrgTreeTestHost.BaseTime.AddMinutes(i), spaceId: space));
            return Task.CompletedTask;
        });

        var overview = await GetAsync(stack, stack.OwnerId, space);

        overview.BoardTotal.Should().Be(8);
        overview.Boards.Should().HaveCount(6);
        overview.DocTotal.Should().Be(6);
        overview.Docs.Should().HaveCount(4);
        overview.Docs.Select(d => d.Title).Should().Equal("Doc 5", "Doc 4", "Doc 3", "Doc 2");
        overview.Workload.Should().HaveCount(5);
    }

    private Task SeedViewAsync(OrgStack stack, Guid board, ViewType type, bool isDefault) =>
        _host.WithSystemContextAsync(c =>
        {
            BoardViewConfig config = type == ViewType.Kanban
                ? KanbanViewConfig.Create(
                    BoardField.Create(stack.AccountId, stack.WorkspaceId, board, "Status", FieldType.Status, FieldSettings.Create(JsonValue.Create("{\"transitions\":[]}")), FractionalIndex.Create("a0"), stack.OwnerId, OrgTreeTestHost.BaseTime),
                    [Guid.NewGuid()])
                : TableViewConfig.Create(JsonValue.EmptyObject());
            c.BoardViews.Add(BoardView.Create(stack.AccountId, stack.WorkspaceId, board, $"{type} view", type, config, stack.OwnerId, OrgTreeTestHost.BaseTime, isDefault));
            return Task.CompletedTask;
        });

    private Task<Guid> SeedSpaceAsync(OrgStack stack, params Guid[] members) =>
        _host.SeedSpaceAsync(stack, $"Dept {Guid.NewGuid():N}"[..12], SpaceVisibility.Workspace, stack.OwnerId, type: SpaceType.Department, transition: s =>
        {
            foreach (var m in members)
                s.AddMember(SpaceMemberSubjectType.User, m, SpaceRole.Member, stack.OwnerId, OrgTreeTestHost.BaseTime);
        });

    /// <param name="items">Name, done, due in days from now (null = none), assigned to <paramref name="assignee"/>.</param>
    private async Task<Guid> SeedBoardWithItemsAsync(
        OrgStack stack, string title, Guid? space, Guid assignee,
        params (string Name, bool Done, int? DueDays, bool Assigned)[] items) =>
        await SeedBoardWithItemsAsync(stack, title, space, assignee, items, BoardVisibility.Workspace);

    private async Task<Guid> SeedBoardWithItemsAsync(
        OrgStack stack, string title, Guid? space, Guid assignee,
        (string Name, bool Done, int? DueDays, bool Assigned) item1,
        (string Name, bool Done, int? DueDays, bool Assigned) item2,
        BoardVisibility visibility = BoardVisibility.Workspace) =>
        await SeedBoardWithItemsAsync(stack, title, space, assignee, [item1, item2], visibility);

    private async Task<Guid> SeedBoardWithItemsAsync(
        OrgStack stack, string title, Guid? space, Guid assignee,
        (string Name, bool Done, int? DueDays, bool Assigned) item1,
        BoardVisibility visibility)
        => await SeedBoardWithItemsAsync(stack, title, space, assignee, [item1], visibility);

    private async Task<Guid> SeedBoardWithItemsAsync(
        OrgStack stack, string title, Guid? space, Guid assignee,
        (string Name, bool Done, int? DueDays, bool Assigned)[] items,
        BoardVisibility visibility)
    {
        var board = await _host.SeedBoardAsync(stack, title, visibility, space);
        await _host.WithSystemContextAsync(c =>
        {
            var group = BoardGroup.Create(stack.AccountId, stack.WorkspaceId, board, "Group", Color.Create("#0079BF"), FractionalIndex.Create("a0"), stack.OwnerId, OrgTreeTestHost.BaseTime);
            c.BoardGroups.Add(group);
            var index = 0;
            foreach (var (name, done, dueDays, assigned) in items)
            {
                var item = BoardItem.CreateRoot(stack.AccountId, stack.WorkspaceId, board, group.Id, name, FractionalIndex.Create($"a{++index}"), stack.OwnerId, OrgTreeTestHost.BaseTime);
                if (dueDays is { } days)
                    item.SetTimeline(null, _host.Now.AddDays(days), stack.OwnerId, _host.Now);
                if (done)
                    item.Complete(_host.Now, stack.OwnerId, _host.Now);
                c.BoardItems.Add(item);
                if (assigned && !done)
                    c.BoardItemMembers.Add(BoardItemMember.Create(stack.AccountId, stack.WorkspaceId, board, item.Id, assignee, stack.OwnerId, OrgTreeTestHost.BaseTime));
            }

            return Task.CompletedTask;
        });
        return board;
    }

    private async Task<SpaceOverviewDto> GetAsync(OrgStack stack, Guid userId, Guid spaceId, bool asMember = false)
    {
        using var provider = _host.CreateProvider(stack.AccountId, userId, services =>
        {
            services.AddScoped<IBoardAccessSubjectFacts, BoardAccessSubjectFactsAdapter>();
            services.AddScoped<IResourceAccessEntriesReader, PostgresResourceAccessEntriesReader>();
            services.AddScoped<IResourceAccessSummary, ResourceAccessSummaryService>();
            services.AddScoped<IBoardSharingSummary>(sp => (ResourceAccessSummaryService)sp.GetRequiredService<IResourceAccessSummary>());
        });
        return await OrgTreeTestHost.QueryAsync(provider, new GetSpaceOverviewQuery(stack.WorkspaceId, spaceId, asMember));
    }
}
