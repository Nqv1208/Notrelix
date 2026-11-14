using Notrelix.Application.Features.Collaboration.Public.Notifications;
using Notrelix.Application.Features.WorkManagement.Ports.Collaboration;
using Notrelix.Application.Features.Workspaces.Public.Teams;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetHomeFeed;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetMyWorkExtended;
using Notrelix.Domain.SharedKernel.Ordering;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class MyWorkAndHomeFeedTests : WorkManagementHandlerTestBase
{
    private readonly FakeResourceVisibility _visibility = new();
    private readonly Mock<IActorLookupService> _actors = new();
    private readonly Mock<INotificationInbox> _inbox = new();
    private readonly Mock<IWorkManagementCollaborationReadPort> _collaboration = new();
    private readonly InMemoryBoardItemReminders _reminders = new();
    private readonly Mock<ITeamMembershipFacts> _teams = new();

    public MyWorkAndHomeFeedTests()
    {
        _actors
            .Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Guid> ids, CancellationToken _) =>
                ids.Select(id => new ActorSnapshot(id, "Person " + id.ToString("N")[..4], null)).ToList());
        _collaboration
            .Setup(c => c.GetFollowedItemIdsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private GetMyWorkExtendedQueryHandler MyWork() =>
        new(DbContextMock.Object, RequestContextMock.Object, _visibility, _actors.Object, _collaboration.Object, _reminders, _teams.Object);

    private GetHomeFeedQueryHandler Home() =>
        new(DbContextMock.Object, RequestContextMock.Object, _visibility, _inbox.Object, _actors.Object, _teams.Object);

    private BoardField StatusFieldFor(Guid boardId)
    {
        var field = BoardField.Create(
            TestAccountId, TestWorkspaceId, boardId, "Status", FieldType.Status,
            FieldSettings.Create(JsonValue.Create("{\"transitions\":{}}")!),
            FractionalIndex.Create("a1"), TestUserId, TestNow, isSystem: true);
        field.AddDefaultStatusOptions(TestUserId, TestNow);
        return field;
    }

    [Fact]
    public async Task MyWork_ReturnsOpenAndDoneItemsWithStatusKeyAndAssignee()
    {
        var board = CreateBoard();
        SetupBoards(board);
        _visibility.Boards.Add(board.Id);
        var status = StatusFieldFor(board.Id);
        SetupBoardFields(status);
        var doing = status.Options.First(o => o.Category == WorkStatusCategory.InProgress);

        var open = CreateBoardItem(boardId: board.Id);
        var done = CreateBoardItem(boardId: board.Id);
        done.Complete(TestNow, TestUserId, TestNow);
        var stranger = CreateBoardItem(boardId: board.Id);
        SetupBoardItems(open, done, stranger);
        var colleague = Guid.CreateVersion7();
        SetupBoardItemMembers(
            CreateBoardItemMember(itemId: open.Id, userId: TestUserId),
            CreateBoardItemMember(itemId: open.Id, userId: colleague),
            CreateBoardItemMember(itemId: done.Id, userId: TestUserId));
        DbContextMock.Setup(c => c.BoardItemValues).Returns(CreateAsyncDbSet(new List<BoardItemValue>
        {
            BoardItemValue.Create(open.Id, status.Id, FieldValue.Create(JsonValue.Create($"\"{doing.Id}\"")!)),
        }));

        var result = await MyWork().Handle(new GetMyWorkExtendedQuery(TestWorkspaceId), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Items.Should().ContainSingle();
        var dto = result.Data.Items[0];
        dto.Id.Should().Be(open.Id);
        dto.Status.Should().NotBeNull();
        dto.Status!.OptionId.Should().Be(doing.Id);
        dto.Status.Bucket.Should().Be("doing");
        dto.Relations.Should().Equal("assigned");
        dto.Assignee!.Id.Should().Be(colleague);
        result.Data.Done.Should().ContainSingle(d => d.Id == done.Id && d.CompletedAt != null && d.Assignee == null);
        result.Data.Waiting.Should().BeEmpty();
        result.Data.Departments.Should().Equal(new Dictionary<string, string?> { [board.Id.ToString()] = null });
    }

    [Fact]
    public async Task MyWork_NamesTheSpaceEachShownBoardIsPlacedIn()
    {
        var placed = CreateBoard();
        var spaceId = Guid.CreateVersion7();
        placed.PlaceInSpace(spaceId, TestUserId, TestNow);
        var loose = CreateBoard();
        var unused = CreateBoard();
        SetupBoards(placed, loose, unused);
        _visibility.Boards.Add(placed.Id);
        _visibility.Boards.Add(loose.Id);
        _visibility.Boards.Add(unused.Id);
        var inPlaced = CreateBoardItem(boardId: placed.Id);
        var inLoose = CreateBoardItem(boardId: loose.Id);
        SetupBoardItems(inPlaced, inLoose);
        SetupBoardItemMembers(
            CreateBoardItemMember(itemId: inPlaced.Id, userId: TestUserId),
            CreateBoardItemMember(itemId: inLoose.Id, userId: TestUserId));

        var result = await MyWork().Handle(new GetMyWorkExtendedQuery(TestWorkspaceId), CancellationToken.None);

        result.Data!.Departments.Should().Equal(new Dictionary<string, string?>
        {
            [placed.Id.ToString()] = spaceId.ToString(),
            [loose.Id.ToString()] = null,
        });
    }

    [Fact]
    public async Task MyWork_ListsTheOpenItemsTheCallerFollowsWithTheFollowingRelation()
    {
        var board = CreateBoard();
        var hiddenBoard = CreateBoard();
        SetupBoards(board, hiddenBoard);
        _visibility.Boards.Add(board.Id);
        var assignedAndFollowed = CreateBoardItem(boardId: board.Id);
        var followedOnly = CreateBoardItem(boardId: board.Id);
        var followedDone = CreateBoardItem(boardId: board.Id);
        followedDone.Complete(TestNow, TestUserId, TestNow);
        var followedHidden = CreateBoardItem(boardId: hiddenBoard.Id);
        SetupBoardItems(assignedAndFollowed, followedOnly, followedDone, followedHidden);
        SetupBoardItemMembers(CreateBoardItemMember(itemId: assignedAndFollowed.Id, userId: TestUserId));
        _collaboration
            .Setup(c => c.GetFollowedItemIdsAsync(TestWorkspaceId, TestUserId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([followedOnly.Id, assignedAndFollowed.Id, followedDone.Id, followedHidden.Id]);

        var result = await MyWork().Handle(new GetMyWorkExtendedQuery(TestWorkspaceId), CancellationToken.None);

        result.Data!.Items.Select(i => i.Id).Should().BeEquivalentTo([assignedAndFollowed.Id, followedOnly.Id]);
        result.Data.Items.Single(i => i.Id == assignedAndFollowed.Id).Relations.Should().Equal("assigned", "following");
        result.Data.Items.Single(i => i.Id == followedOnly.Id).Relations.Should().Equal("following");
        result.Data.Done.Should().BeEmpty();
    }

    [Fact]
    public async Task MyWork_HidesItemsOnBoardsTheCallerCannotRead()
    {
        var board = CreateBoard();
        SetupBoards(board);
        var item = CreateBoardItem(boardId: board.Id);
        SetupBoardItems(item);
        SetupBoardItemMembers(CreateBoardItemMember(itemId: item.Id, userId: TestUserId));

        var result = await MyWork().Handle(new GetMyWorkExtendedQuery(TestWorkspaceId), CancellationToken.None);

        result.Data!.Items.Should().BeEmpty();
        result.Data.Done.Should().BeEmpty();
    }

    [Fact]
    public async Task HomeFeed_ShowsUnreadMentionsOfReadableItems_AndDropsTheRest()
    {
        var board = CreateBoard();
        var hiddenBoard = CreateBoard();
        _visibility.Boards.Add(board.Id);
        var readable = CreateBoardItem(boardId: board.Id);
        var hidden = CreateBoardItem(boardId: hiddenBoard.Id);
        SetupBoardItems(readable, hidden);
        var actorId = Guid.CreateVersion7();
        _inbox
            .Setup(i => i.RecentUnreadMentionsAsync(TestWorkspaceId, TestUserId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UnreadMentionNotification>
            {
                new(Guid.CreateVersion7(), actorId, TestNow, "You were mentioned", "work-management.board-item", readable.Id),
                new(Guid.CreateVersion7(), actorId, TestNow, "You were mentioned", "work-management.board-item", hidden.Id),
                new(Guid.CreateVersion7(), actorId, TestNow, "You were mentioned", "documents.page", Guid.CreateVersion7()),
            });

        var result = await Home().Handle(new GetHomeFeedQuery(TestWorkspaceId), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Replies.Should().ContainSingle();
        result.Data.Replies[0].Target.Id.Should().Be(readable.Id);
        result.Data.Replies[0].Target.BoardId.Should().Be(board.Id);
        result.Data.Replies[0].Actor.Id.Should().Be(actorId);
        result.Data.Summary.MentionsUnread.Should().Be(1);
        result.Data.Summary.ApprovalsPending.Should().Be(0);
        result.Data.Events.Should().BeEmpty();
    }

    [Fact]
    public async Task MyWork_Waiting_ListsOpenItemsTheCallerCreatedOrFollowsThatOnlyOthersAreAssignedTo()
    {
        var board = CreateBoard();
        var created = CreateBoardItem(boardId: board.Id);
        var followed = CreateBoardItem(boardId: board.Id);
        var assignedToCaller = CreateBoardItem(boardId: board.Id);
        var unassigned = CreateBoardItem(boardId: board.Id);
        SetupBoards(board);
        _visibility.Boards.Add(board.Id);
        SetupBoardItems(created, followed, assignedToCaller, unassigned);
        var phuc = Guid.CreateVersion7();
        SetupBoardItemMembers(
            CreateBoardItemMember(itemId: created.Id, userId: phuc),
            CreateBoardItemMember(itemId: followed.Id, userId: phuc),
            CreateBoardItemMember(itemId: assignedToCaller.Id, userId: phuc),
            CreateBoardItemMember(itemId: assignedToCaller.Id, userId: TestUserId));
        _collaboration
            .Setup(c => c.GetFollowedItemIdsAsync(TestWorkspaceId, TestUserId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([followed.Id]);
        _reminders.Seed(created.Id, TestUserId, TestNow);

        var result = await MyWork().Handle(new GetMyWorkExtendedQuery(TestWorkspaceId), CancellationToken.None);

        result.Data!.Waiting.Select(w => w.ItemId).Should().BeEquivalentTo([created.Id, followed.Id]);
        var entry = result.Data.Waiting.Single(w => w.ItemId == created.Id);
        entry.Owner.Id.Should().Be(phuc);
        entry.LastRemindedAt.Should().Be(TestNow);
        result.Data.Waiting.Single(w => w.ItemId == followed.Id).LastRemindedAt.Should().BeNull();
    }

    [Fact]
    public async Task MyWork_Waiting_SortsBySoonestDueUndatedLast_AndSkipsFinishedItemsAndHiddenBoards()
    {
        var board = CreateBoard();
        var hidden = CreateBoard();
        var late = CreateBoardItem(boardId: board.Id);
        var soon = CreateBoardItem(boardId: board.Id);
        var undated = CreateBoardItem(boardId: board.Id);
        var finished = CreateBoardItem(boardId: board.Id);
        var offLimits = CreateBoardItem(boardId: hidden.Id);
        late.SetTimeline(null, TestNow.AddDays(9), TestUserId, TestNow);
        soon.SetTimeline(null, TestNow.AddDays(1), TestUserId, TestNow);
        finished.Complete(TestNow, TestUserId, TestNow);
        SetupBoards(board, hidden);
        _visibility.Boards.Add(board.Id);
        SetupBoardItems(late, soon, undated, finished, offLimits);
        var other = Guid.CreateVersion7();
        SetupBoardItemMembers(new[] { late, soon, undated, finished, offLimits }
            .Select(i => CreateBoardItemMember(itemId: i.Id, userId: other)).ToArray());

        var result = await MyWork().Handle(new GetMyWorkExtendedQuery(TestWorkspaceId), CancellationToken.None);

        result.Data!.Waiting.Select(w => w.ItemId).Should().Equal(soon.Id, late.Id, undated.Id);
    }

    // D76.17: My Work lists the not-done items assigned to me, on boards I can view, ordered by due date with the
    // undated ones last; a done item and an item only someone else is assigned to never appear in it.
    [Fact]
    public async Task MyWork_D76_17_ListsOnlyMyNotDoneItemsByDueDateUndatedLast()
    {
        var board = CreateBoard();
        var hidden = CreateBoard();
        var late = CreateBoardItem(boardId: board.Id);
        var soon = CreateBoardItem(boardId: board.Id);
        var middle = CreateBoardItem(boardId: board.Id);
        var undated = CreateBoardItem(boardId: board.Id);
        var done = CreateBoardItem(boardId: board.Id);
        var othersOnly = CreateBoardItem(boardId: board.Id);
        var offLimits = CreateBoardItem(boardId: hidden.Id);
        late.SetTimeline(null, TestNow.AddDays(9), TestUserId, TestNow);
        soon.SetTimeline(null, TestNow.AddDays(-2), TestUserId, TestNow);
        middle.SetTimeline(TestNow.AddDays(1), TestNow.AddDays(3), TestUserId, TestNow);
        done.SetTimeline(null, TestNow.AddDays(-5), TestUserId, TestNow);
        done.Complete(TestNow, TestUserId, TestNow);
        othersOnly.SetTimeline(null, TestNow.AddDays(-9), TestUserId, TestNow);
        offLimits.SetTimeline(null, TestNow.AddDays(-9), TestUserId, TestNow);
        SetupBoards(board, hidden);
        _visibility.Boards.Add(board.Id);
        SetupBoardItems(late, undated, middle, done, othersOnly, soon, offLimits);
        var other = Guid.CreateVersion7();
        SetupBoardItemMembers(
            CreateBoardItemMember(itemId: late.Id, userId: TestUserId),
            CreateBoardItemMember(itemId: undated.Id, userId: TestUserId),
            CreateBoardItemMember(itemId: middle.Id, userId: TestUserId),
            CreateBoardItemMember(itemId: middle.Id, userId: other),
            CreateBoardItemMember(itemId: done.Id, userId: TestUserId),
            CreateBoardItemMember(itemId: othersOnly.Id, userId: other),
            CreateBoardItemMember(itemId: soon.Id, userId: TestUserId),
            CreateBoardItemMember(itemId: offLimits.Id, userId: TestUserId));

        var result = await MyWork().Handle(new GetMyWorkExtendedQuery(TestWorkspaceId), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Items.Select(i => i.Id).Should().Equal(soon.Id, middle.Id, late.Id, undated.Id);
        result.Data.Items.Should().OnlyContain(i => i.Relations.Contains("assigned"));
        result.Data.Done.Select(d => d.Id).Should().Equal(done.Id);
    }

    private ApprovalRequest TeamAsk(BoardItem item, Guid teamId)
    {
        var target = ResourceRef.Create(ResourceKind.Create("work-management.board-item"), item.Id, TestWorkspaceId);
        var request = ApprovalRequest.Create(TestAccountId, TestWorkspaceId, target, "Team review", Guid.CreateVersion7(), TestNow);
        request.AddStep(1, TestUserId, TestNow, approverTeamId: teamId);
        SetupApprovalRequests(request);
        return request;
    }

    private async Task<int> PendingForCallerAsync(params Guid[] callerTeams)
    {
        _teams
            .Setup(t => t.CurrentTeamIdsAsync(TestWorkspaceId, TestUserId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid _, IReadOnlyCollection<Guid> asked, CancellationToken _) => asked.Where(callerTeams.Contains).ToList());
        _inbox
            .Setup(i => i.RecentUnreadMentionsAsync(TestWorkspaceId, TestUserId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UnreadMentionNotification>());
        var result = await Home().Handle(new GetHomeFeedQuery(TestWorkspaceId), CancellationToken.None);
        return result.Data!.Summary.ApprovalsPending;
    }

    [Fact]
    public async Task HomeFeed_ACurrentTeamMember_SeesTheRequestAskedOfTheirTeam()
    {
        var board = CreateBoard();
        _visibility.Boards.Add(board.Id);
        var item = CreateBoardItem(boardId: board.Id);
        SetupBoardItems(item);
        var team = Guid.CreateVersion7();
        TeamAsk(item, team);

        (await PendingForCallerAsync(team)).Should().Be(1);
    }

    [Fact]
    public async Task HomeFeed_ANonMemberOrAFailedMembershipRead_SeesNothingForTheTeam()
    {
        var board = CreateBoard();
        _visibility.Boards.Add(board.Id);
        var item = CreateBoardItem(boardId: board.Id);
        SetupBoardItems(item);
        TeamAsk(item, Guid.CreateVersion7());

        (await PendingForCallerAsync()).Should().Be(0);
    }

    [Fact]
    public async Task MyWork_ACurrentTeamMember_ListsTheItemTheirTeamIsAskedToApprove()
    {
        var board = CreateBoard();
        SetupBoards(board);
        _visibility.Boards.Add(board.Id);
        SetupBoardFields(StatusFieldFor(board.Id));
        var item = CreateBoardItem(boardId: board.Id);
        SetupBoardItems(item);
        var team = Guid.CreateVersion7();
        TeamAsk(item, team);
        _teams
            .Setup(t => t.CurrentTeamIdsAsync(TestWorkspaceId, TestUserId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([team]);

        var result = await MyWork().Handle(new GetMyWorkExtendedQuery(TestWorkspaceId), CancellationToken.None);

        result.Data!.Items.Should().Contain(i => i.Id == item.Id);
    }
}
