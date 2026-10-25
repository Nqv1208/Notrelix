using Notrelix.Application.Features.Collaboration.Notifications.Commands.MarkInboxEntryRead;
using Notrelix.Application.Features.Collaboration.Notifications.Commands.MarkInboxEntryDone;
using Notrelix.Application.Features.Collaboration.Notifications.Commands.MarkWorkspaceInboxRead;
using Notrelix.Application.Features.Collaboration.Notifications.Commands.MuteInboxThread;
using Notrelix.Application.Features.Collaboration.Notifications.Commands.ReopenInboxEntry;
using Notrelix.Application.Features.Collaboration.Notifications.Commands.SnoozeInboxEntry;
using Notrelix.Application.Features.Collaboration.Public.Notifications;
using Notrelix.Application.Features.WorkManagement.BoardItems.Queries.GetWorkspaceInbox;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

public class WorkspaceInboxTests : WorkManagementHandlerTestBase
{
    private const string ItemKind = "work-management.board-item";
    private readonly FakeResourceVisibility _visibility = new();
    private readonly Mock<IActorLookupService> _actors = new();
    private readonly Mock<INotificationInbox> _inbox = new();

    public WorkspaceInboxTests()
    {
        _actors
            .Setup(a => a.FindManyAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Guid> ids, CancellationToken _) =>
                ids.Select(id => new ActorSnapshot(id, "Person " + id.ToString("N")[..4], null)).ToList());
    }

    private GetWorkspaceInboxQueryHandler Handler() =>
        new(DbContextMock.Object, RequestContextMock.Object, _visibility, _inbox.Object, _actors.Object);

    private static InboxNotification Note(Guid? actor, Guid? resource, string? kind = ItemKind, bool unread = true) =>
        new(Guid.CreateVersion7(), "mention.created", actor, DateTimeOffset.UtcNow, "Mention", "You were mentioned", kind, resource, unread);

    [Fact]
    public async Task Inbox_ShowsNotificationsOfReadableItemsWithTheirTarget_AndDropsAnUnreadableOne()
    {
        var board = CreateBoard();
        var hiddenBoard = CreateBoard();
        _visibility.Boards.Add(board.Id);
        var readable = CreateBoardItem(boardId: board.Id);
        var hidden = CreateBoardItem(boardId: hiddenBoard.Id);
        SetupBoardItems(readable, hidden);
        var actorId = Guid.CreateVersion7();
        _inbox
            .Setup(i => i.ListAsync(TestWorkspaceId, TestUserId, InboxView.Inbox, null, null, GetWorkspaceInboxQuery.ScanLimit, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InboxNotification>
            {
                Note(actorId, readable.Id),
                Note(actorId, hidden.Id),
                Note(actorId, null, kind: null, unread: false),
            });

        var result = await Handler().Handle(new GetWorkspaceInboxQuery(TestWorkspaceId, false, null, null), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.Items[0].Target!.Id.Should().Be(readable.Id);
        result.Data.Items[0].Target!.BoardId.Should().Be(board.Id);
        result.Data.Items[0].Actor!.Id.Should().Be(actorId);
        result.Data.Items[1].Target.Should().BeNull();
        result.Data.Items[1].Unread.Should().BeFalse();
        result.Data.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task Inbox_IsBounded_AndOffersACursorThatContinuesAfterTheLastShownEntry()
    {
        var notes = Enumerable.Range(0, 5).Select(_ => Note(null, null, kind: null)).ToList();
        _inbox
            .Setup(i => i.ListAsync(TestWorkspaceId, TestUserId, InboxView.Unread, null, null, GetWorkspaceInboxQuery.ScanLimit, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notes);
        var handler = Handler();

        var first = await handler.Handle(new GetWorkspaceInboxQuery(TestWorkspaceId, true, null, 2), CancellationToken.None);

        first.Data!.Items.Select(i => i.Id).Should().Equal(notes.Take(2).Select(n => n.NotificationId));
        first.Data.NextCursor.Should().NotBeNullOrEmpty();

        _inbox
            .Setup(i => i.ListAsync(TestWorkspaceId, TestUserId, InboxView.Unread, It.Is<DateTimeOffset?>(d => d == notes[1].CreatedAt), notes[1].NotificationId, GetWorkspaceInboxQuery.ScanLimit, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notes.Skip(2).ToList());
        var second = await handler.Handle(new GetWorkspaceInboxQuery(TestWorkspaceId, true, first.Data.NextCursor, 2), CancellationToken.None);

        second.Data!.Items.Select(i => i.Id).Should().Equal(notes.Skip(2).Take(2).Select(n => n.NotificationId));
    }

    [Fact]
    public async Task Inbox_RejectsACursorItDidNotIssue()
    {
        var result = await Handler().Handle(new GetWorkspaceInboxQuery(TestWorkspaceId, false, "not-a-cursor", null), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        _inbox.Verify(i => i.ListAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<InboxView>(), It.IsAny<DateTimeOffset?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MarkRead_ActsOnlyForTheCaller_AndMarkAllAnswersTheCount()
    {
        var notificationId = Guid.CreateVersion7();
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(TestNow);
        _inbox.Setup(i => i.MarkAllReadAsync(TestWorkspaceId, TestUserId, TestNow, It.IsAny<CancellationToken>())).ReturnsAsync(3);

        var one = await new MarkInboxEntryReadCommandHandler(_inbox.Object, RequestContextMock.Object, clock.Object)
            .Handle(new MarkInboxEntryReadCommand(TestWorkspaceId, notificationId), CancellationToken.None);
        var all = await new MarkWorkspaceInboxReadCommandHandler(_inbox.Object, RequestContextMock.Object, clock.Object)
            .Handle(new MarkWorkspaceInboxReadCommand(TestWorkspaceId), CancellationToken.None);

        one.Succeeded.Should().BeTrue();
        _inbox.Verify(i => i.MarkReadAsync(TestWorkspaceId, TestUserId, notificationId, TestNow, It.IsAny<CancellationToken>()), Times.Once);
        all.Data!.Marked.Should().Be(3);
    }

    [Fact]
    public async Task Inbox_StateDone_ListsTheDoneView_AndCarriesTheNeedsReplyMuteAndSnoozeFacts()
    {
        var until = TestNow.AddHours(3);
        _inbox
            .Setup(i => i.ListAsync(TestWorkspaceId, TestUserId, InboxView.Done, null, null, GetWorkspaceInboxQuery.ScanLimit, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InboxNotification>
            {
                Note(null, null, kind: null) with { NeedsReply = true, Muted = true, SnoozedUntil = until },
            });

        var result = await Handler().Handle(new GetWorkspaceInboxQuery(TestWorkspaceId, false, null, null, "done"), CancellationToken.None);

        var entry = result.Data!.Items.Should().ContainSingle().Subject;
        entry.NeedsReply.Should().BeTrue();
        entry.Muted.Should().BeTrue();
        entry.SnoozedUntil.Should().Be(until);
    }

    [Fact]
    public async Task Inbox_StateSnoozed_ListsTheSnoozedView()
    {
        _inbox
            .Setup(i => i.ListAsync(TestWorkspaceId, TestUserId, InboxView.Snoozed, null, null, GetWorkspaceInboxQuery.ScanLimit, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InboxNotification>());

        var result = await Handler().Handle(new GetWorkspaceInboxQuery(TestWorkspaceId, false, null, null, "snoozed"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _inbox.VerifyAll();
    }

    [Fact]
    public void InboxQuery_RejectsAnUnknownState()
    {
        var validator = new GetWorkspaceInboxQueryValidator();

        validator.Validate(new GetWorkspaceInboxQuery(TestWorkspaceId, false, null, null, "archived")).IsValid.Should().BeFalse();
        validator.Validate(new GetWorkspaceInboxQuery(TestWorkspaceId, false, null, null, "Done")).IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Snooze_RefusesATimeInThePastOrBeyondAYear_AndOtherwiseHidesTheEntryUntilThen()
    {
        var notificationId = Guid.CreateVersion7();
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(TestNow);
        var handler = new SnoozeInboxEntryCommandHandler(_inbox.Object, RequestContextMock.Object, clock.Object);

        var past = await handler.Handle(new SnoozeInboxEntryCommand(TestWorkspaceId, notificationId, TestNow.AddMinutes(-1)), CancellationToken.None);
        var tooFar = await handler.Handle(new SnoozeInboxEntryCommand(TestWorkspaceId, notificationId, TestNow.AddDays(400)), CancellationToken.None);
        var until = TestNow.AddHours(4);
        var ok = await handler.Handle(new SnoozeInboxEntryCommand(TestWorkspaceId, notificationId, until), CancellationToken.None);

        past.Succeeded.Should().BeFalse();
        tooFar.Succeeded.Should().BeFalse();
        ok.Succeeded.Should().BeTrue();
        _inbox.Verify(i => i.SnoozeAsync(TestWorkspaceId, TestUserId, notificationId, until, TestNow, It.IsAny<CancellationToken>()), Times.Once);
        _inbox.Verify(i => i.SnoozeAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DoneReopenAndMute_ActOnlyForTheCaller()
    {
        var notificationId = Guid.CreateVersion7();
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.UtcNow).Returns(TestNow);

        await new MarkInboxEntryDoneCommandHandler(_inbox.Object, RequestContextMock.Object, clock.Object)
            .Handle(new MarkInboxEntryDoneCommand(TestWorkspaceId, notificationId), CancellationToken.None);
        await new ReopenInboxEntryCommandHandler(_inbox.Object, RequestContextMock.Object, clock.Object)
            .Handle(new ReopenInboxEntryCommand(TestWorkspaceId, notificationId), CancellationToken.None);
        var mute = new MuteInboxThreadCommandHandler(_inbox.Object, RequestContextMock.Object, clock.Object);
        await mute.Handle(new MuteInboxThreadCommand(TestWorkspaceId, notificationId, true), CancellationToken.None);
        await mute.Handle(new MuteInboxThreadCommand(TestWorkspaceId, notificationId, false), CancellationToken.None);

        _inbox.Verify(i => i.MarkDoneAsync(TestWorkspaceId, TestUserId, notificationId, TestNow, It.IsAny<CancellationToken>()), Times.Once);
        _inbox.Verify(i => i.ReopenAsync(TestWorkspaceId, TestUserId, notificationId, TestNow, It.IsAny<CancellationToken>()), Times.Once);
        _inbox.Verify(i => i.MuteThreadAsync(TestWorkspaceId, TestUserId, notificationId, TestNow, It.IsAny<CancellationToken>()), Times.Once);
        _inbox.Verify(i => i.UnmuteThreadAsync(TestWorkspaceId, TestUserId, notificationId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
