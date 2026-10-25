using Notrelix.Application.Features.Collaboration.Public.Notifications;
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

    public MyWorkAndHomeFeedTests()
    {
        _actors
            .Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Guid> ids, CancellationToken _) =>
                ids.Select(id => new ActorSnapshot(id, "Person " + id.ToString("N")[..4], null)).ToList());
    }

    private GetMyWorkExtendedQueryHandler MyWork() =>
        new(DbContextMock.Object, RequestContextMock.Object, _visibility, _actors.Object);

    private GetHomeFeedQueryHandler Home() =>
        new(DbContextMock.Object, RequestContextMock.Object, _visibility, _inbox.Object, _actors.Object);

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
        result.Data.Departments.Should().BeEmpty();
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
}
