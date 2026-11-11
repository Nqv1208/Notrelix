using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Messages.Queries.ListMessages;
using Notrelix.Application.Features.Chat.Threads.Commands.FollowThread;
using Notrelix.Application.Features.Chat.Threads.Commands.MarkThreadRead;
using Notrelix.Application.Features.Chat.Threads.Commands.ReplyInThread;
using Notrelix.Application.Features.Chat.Threads.Queries.ListFollowedThreads;
using Notrelix.Application.Features.Chat.Threads.Queries.ListThreadReplies;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Application.Tests.Features.Collaboration;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Tests.Features.Chat.Threads;

/// <summary>
/// Thread use cases UC-CHT-F01, F03, F04, F08 and the thread part of B02 at handler level, with fakes. The row lock and the
/// database's re-read are proved against PostgreSQL in the integration tests; here the lock order and the re-read after it
/// are asserted through the fake allocator.
/// </summary>
public class ThreadHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private readonly Guid _other = Guid.NewGuid();
    private readonly Guid _outsider = Guid.NewGuid();

    private readonly Mock<ICurrentRequestContext> _context = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IWorkspaceMemberFacts> _members = new();
    private readonly List<Space> _spaces = [];
    private readonly List<Message> _messages = [];
    private readonly List<MessageThread> _threads = [];
    private readonly List<ThreadFollower> _followers = [];
    private readonly List<string> _order = [];
    private readonly Mock<IChatDbContext> _db = new();
    private readonly Mock<ISpaceSequenceAllocator> _sequences = new();
    private readonly FakeThreadReadStore _readStore;
    private long _lastSequence;

    public ThreadHandlerTests()
    {
        _readStore = new FakeThreadReadStore(_followers);

        _clock.Setup(c => c.UtcNow).Returns(() => Now);
        _context.Setup(c => c.RequireAccountId()).Returns(_accountId);
        _context.Setup(c => c.RequireWorkspaceId()).Returns(_workspaceId);
        _context.Setup(c => c.IsAuthenticated).Returns(true);
        _context.Setup(c => c.UserId).Returns(_member);

        _db.Setup(d => d.ChatSpaces).Returns(() => TestDbSet.Create(_spaces).Object);
        _db.Setup(d => d.ChatMemberships).Returns(() => TestDbSet.Create(_spaces.SelectMany(s => s.Memberships).ToList()).Object);
        _db.Setup(d => d.ChatChannelGroups).Returns(TestDbSet.Create(new List<ChannelGroup>()).Object);
        _db.Setup(d => d.ChatMessages).Returns(() => TestDbSet.Create(_messages).Object);
        _db.Setup(d => d.ChatThreads).Returns(() => TestDbSet.Create(_threads).Object);
        _db.Setup(d => d.ChatThreadFollowers).Returns(() => TestDbSet.Create(_followers).Object);

        _sequences.Setup(s => s.LockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback(() => _order.Add("lock"))
            .Returns(Task.CompletedTask);
        _sequences.Setup(s => s.NextAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => { _order.Add("next"); return ++_lastSequence; });

        _members.Setup(m => m.FindAsync(_workspaceId, _owner, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_owner, "Owner", true));
        _members.Setup(m => m.FindAsync(_workspaceId, _member, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_member, "Member", true));
        _members.Setup(m => m.FindAsync(_workspaceId, _other, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_other, "Member", true));
        _members.Setup(m => m.FindAsync(_workspaceId, _outsider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_outsider, "Member", true));
    }

    private void AsUser(Guid userId) => _context.Setup(c => c.UserId).Returns(userId);

    private Space Channel(SpaceKind kind = SpaceKind.ChannelPublic, string name = "general")
    {
        var space = Space.CreateChannel(_accountId, _workspaceId, kind, name, null, null, 1, _owner, Now);
        space.AddMember(_member, MembershipRole.Member, Now);
        space.AddMember(_other, MembershipRole.Member, Now);
        _spaces.Add(space);
        return space;
    }

    private Message Root(Space space, Guid? author = null, bool deleted = false)
    {
        var message = Message.CreateUserMessage(_accountId, _workspaceId, space.Id, ++_lastSequence, author ?? _member,
            "the root", $"root-{Guid.NewGuid():N}", null, Now.AddHours(-2));
        if (deleted) message.Delete(author ?? _member, Now.AddHours(-1));
        _messages.Add(message);
        return message;
    }

    private MessageThread Thread(Space space, Message root, int replies, DateTimeOffset? lastReplyAt = null)
    {
        var thread = MessageThread.Open(_accountId, _workspaceId, space.Id, root.Id, lastReplyAt ?? Now.AddHours(-1));
        for (var i = 0; i < replies; i++) thread.RecordReply(lastReplyAt ?? Now.AddHours(-1));
        _threads.Add(thread);
        return thread;
    }

    private Message Reply(Space space, Message root, long threadSequence, Guid author, string text = "reply")
    {
        var reply = Message.CreateThreadReply(_accountId, _workspaceId, space.Id, ++_lastSequence, root.Id, threadSequence,
            author, text, $"r-{Guid.NewGuid():N}", alsoSentToChannel: false, Now.AddHours(-1));
        _messages.Add(reply);
        return reply;
    }

    private ReplyInThreadCommandHandler ReplyHandler() => new(_db.Object, _context.Object, _members.Object, _sequences.Object, _clock.Object);
    private FollowThreadCommandHandler FollowHandler() => new(_db.Object, _context.Object, _members.Object);
    private MarkThreadReadCommandHandler MarkHandler() => new(_db.Object, _context.Object, _members.Object, _readStore);
    private ListThreadRepliesQueryHandler RepliesHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object);
    private ListFollowedThreadsQueryHandler FollowedHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object);
    private ListMessagesQueryHandler ListMessagesHandler() => new(_db.Object, _context.Object, _members.Object);

    private static ReplyInThreadCommand Command(Guid workspaceId, Guid rootId, string content = "hello", string clientId = "c-1", bool alsoSend = false) =>
        new(workspaceId, rootId, content, clientId, alsoSend);

    // ---- Reply (UC-CHT-F01) ----------------------------------------------------------------------

    [Fact]
    public async Task A_reply_takes_the_space_sequence_and_the_thread_sequence_and_the_lock_comes_first()
    {
        var space = Channel();
        var root = Root(space);

        var result = await ReplyHandler().Handle(Command(_workspaceId, root.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Created.Should().BeTrue();
        result.Data.Message.ThreadRootId.Should().Be(root.Id);
        result.Data.Message.ThreadSequence.Should().Be(1);
        result.Data.Message.Sequence.Should().Be(2, "the root is sequence 1 and the reply takes the next space sequence");
        _order.Should().Equal("lock", "next");
    }

    [Fact]
    public async Task Replies_number_their_thread_gap_free_and_each_one_bumps_the_count()
    {
        var space = Channel();
        var root = Root(space);

        for (var i = 1; i <= 3; i++)
        {
            var result = await ReplyHandler().Handle(Command(_workspaceId, root.Id, $"reply {i}", $"c-{i}"), CancellationToken.None);
            result.Data!.Message.ThreadSequence.Should().Be(i);
        }

        _threads.Should().ContainSingle().Which.ReplyCount.Should().Be(3);
    }

    [Fact]
    public async Task A_retry_with_the_same_client_message_id_returns_the_stored_reply_and_stores_nothing()
    {
        var space = Channel();
        var root = Root(space);
        var first = await ReplyHandler().Handle(Command(_workspaceId, root.Id, clientId: "same"), CancellationToken.None);
        var storedBefore = _messages.Count;

        var retry = await ReplyHandler().Handle(Command(_workspaceId, root.Id, clientId: "same"), CancellationToken.None);

        retry.Succeeded.Should().BeTrue();
        retry.Data!.Created.Should().BeFalse();
        retry.Data.Message.Id.Should().Be(first.Data!.Message.Id);
        _messages.Count.Should().Be(storedBefore, "a retry stores no second reply");
        _threads.Single().ReplyCount.Should().Be(1, "the count does not move on a retry");
    }

    [Fact]
    public async Task A_reply_to_a_reply_is_refused_as_thread_root_invalid()
    {
        var space = Channel();
        var root = Root(space);
        var reply = Reply(space, root, 1, _other);

        var act = () => ReplyHandler().Handle(Command(_workspaceId, reply.Id), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.Message.Should().NotBeNullOrWhiteSpace();
        _messages.Count.Should().Be(2, "nothing was stored");
    }

    [Fact]
    public async Task A_reply_to_a_system_message_is_refused()
    {
        var space = Channel();
        var system = Message.CreateSystemMessage(_accountId, _workspaceId, space.Id, ++_lastSequence, _owner, "joined", Now);
        _messages.Add(system);

        var act = () => ReplyHandler().Handle(Command(_workspaceId, system.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _threads.Should().BeEmpty();
    }

    [Fact]
    public async Task A_reply_to_an_archived_channel_is_refused_with_channel_archived()
    {
        var space = Channel();
        var root = Root(space);
        space.SetArchived(true, _owner, Now);

        var act = () => ReplyHandler().Handle(Command(_workspaceId, root.Id), CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>()).Which.Message.Should().Contain("archived");
        _messages.Count.Should().Be(1);
    }

    [Fact]
    public async Task A_non_member_of_a_public_channel_cannot_reply()
    {
        var space = Channel();
        var root = Root(space);
        AsUser(_outsider);

        var act = () => ReplyHandler().Handle(Command(_workspaceId, root.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>("a reply needs membership, as a send does");
    }

    [Fact]
    public async Task A_reply_to_a_private_thread_by_a_non_member_is_the_same_not_found_as_a_missing_root()
    {
        var space = Channel(SpaceKind.ChannelPrivate, "secret");
        var root = Root(space);
        AsUser(_outsider);

        var missing = () => ReplyHandler().Handle(Command(_workspaceId, Guid.NewGuid()), CancellationToken.None);
        var notMember = () => ReplyHandler().Handle(Command(_workspaceId, root.Id), CancellationToken.None);

        await missing.Should().ThrowAsync<NotFoundException>();
        await notMember.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task A_deleted_root_with_no_replies_is_refused_with_message_deleted()
    {
        var space = Channel();
        var root = Root(space, deleted: true);

        var act = () => ReplyHandler().Handle(Command(_workspaceId, root.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _messages.Count.Should().Be(1);
    }

    [Fact]
    public async Task A_deleted_root_that_has_replies_still_takes_replies()
    {
        var space = Channel();
        var root = Root(space, deleted: true);
        Thread(space, root, replies: 2);

        var result = await ReplyHandler().Handle(Command(_workspaceId, root.Id), CancellationToken.None);

        result.Data!.Message.ThreadSequence.Should().Be(3);
    }

    [Fact]
    public async Task A_channel_archived_after_the_lock_is_refused_because_the_check_is_read_again()
    {
        var space = Channel();
        var root = Root(space);
        _sequences.Setup(s => s.LockAsync(space.Id, It.IsAny<CancellationToken>()))
            .Callback(() => space.SetArchived(true, _owner, Now))
            .Returns(Task.CompletedTask);

        var act = () => ReplyHandler().Handle(Command(_workspaceId, root.Id), CancellationToken.None);

        (await act.Should().ThrowAsync<ConflictException>()).Which.Message.Should().Contain("archived");
        _threads.Should().BeEmpty("nothing was allocated");
    }

    [Fact]
    public async Task A_root_deleted_after_the_lock_is_refused_because_the_root_is_read_again()
    {
        var space = Channel();
        var root = Root(space);
        _sequences.Setup(s => s.LockAsync(space.Id, It.IsAny<CancellationToken>()))
            .Callback(() => root.Delete(root.AuthorId, Now))
            .Returns(Task.CompletedTask);

        var act = () => ReplyHandler().Handle(Command(_workspaceId, root.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        _threads.Should().BeEmpty();
    }

    [Fact]
    public async Task A_reply_reopens_an_archived_thread_and_moves_its_last_reply_time()
    {
        var space = Channel();
        var root = Root(space);
        var thread = Thread(space, root, replies: 1, lastReplyAt: Now.AddDays(-10));
        thread.IsArchivedAt(Now).Should().BeTrue("ten days without a reply");

        await ReplyHandler().Handle(Command(_workspaceId, root.Id), CancellationToken.None);

        thread.ArchivedAt.Should().BeNull();
        thread.LastReplyAt.Should().Be(Now);
        thread.IsArchivedAt(Now).Should().BeFalse();
    }

    [Fact]
    public async Task The_root_author_and_the_replier_follow_the_thread_and_the_replier_has_read_their_reply()
    {
        var space = Channel();
        var root = Root(space, author: _owner);
        AsUser(_member);

        await ReplyHandler().Handle(Command(_workspaceId, root.Id), CancellationToken.None);

        _followers.Should().HaveCount(2);
        _followers.Should().Contain(f => f.UserId == _owner && !f.Muted && f.ReadReplySequence == 0);
        _followers.Should().Contain(f => f.UserId == _member && !f.Muted && f.ReadReplySequence == 1);
    }

    [Fact]
    public async Task A_reply_with_also_send_to_channel_is_stored_with_the_flag()
    {
        var space = Channel();
        var root = Root(space);

        var result = await ReplyHandler().Handle(Command(_workspaceId, root.Id, alsoSend: true), CancellationToken.None);

        _messages.Single(m => m.Id == result.Data!.Message.Id).AlsoSentToChannel.Should().BeTrue();
    }

    // ---- Follow (UC-CHT-F04) ---------------------------------------------------------------------

    [Fact]
    public async Task Following_creates_a_follow_muting_flips_it_and_unfollowing_removes_it()
    {
        var space = Channel();
        var root = Root(space);
        Thread(space, root, replies: 2);
        AsUser(_other);

        var followed = await FollowHandler().Handle(new FollowThreadCommand(_workspaceId, root.Id, true, false), CancellationToken.None);
        followed.Data!.Following.Should().BeTrue();
        _followers.Should().ContainSingle(f => f.UserId == _other && !f.Muted);

        var muted = await FollowHandler().Handle(new FollowThreadCommand(_workspaceId, root.Id, true, true), CancellationToken.None);
        muted.Data!.Muted.Should().BeTrue();
        _followers.Single().Muted.Should().BeTrue();

        var unfollowed = await FollowHandler().Handle(new FollowThreadCommand(_workspaceId, root.Id, false, false), CancellationToken.None);
        unfollowed.Data!.Following.Should().BeFalse();
        _followers.Should().BeEmpty();
    }

    [Fact]
    public async Task A_new_follow_starts_with_the_replies_already_posted_read()
    {
        var space = Channel();
        var root = Root(space);
        Thread(space, root, replies: 4);
        AsUser(_other);

        await FollowHandler().Handle(new FollowThreadCommand(_workspaceId, root.Id, true, false), CancellationToken.None);

        _followers.Single().ReadReplySequence.Should().Be(4);
    }

    [Fact]
    public async Task Following_a_thread_that_has_no_replies_yet_is_not_found()
    {
        var space = Channel();
        var root = Root(space);
        AsUser(_other);

        var act = () => FollowHandler().Handle(new FollowThreadCommand(_workspaceId, root.Id, true, false), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Following_a_private_thread_as_a_non_member_is_not_found()
    {
        var space = Channel(SpaceKind.ChannelPrivate, "secret");
        var root = Root(space);
        Thread(space, root, replies: 1);
        AsUser(_outsider);

        var act = () => FollowHandler().Handle(new FollowThreadCommand(_workspaceId, root.Id, true, false), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _followers.Should().BeEmpty();
    }

    // ---- Mark read (UC-CHT-F08) ------------------------------------------------------------------

    [Fact]
    public async Task Marking_read_moves_the_pointer_forward_only_and_reports_what_is_left()
    {
        var space = Channel();
        var root = Root(space);
        Thread(space, root, replies: 5);
        _followers.Add(ThreadFollower.Follow(_accountId, _workspaceId, root.Id, _member, muted: false, readReplySequence: 1));

        var partway = await MarkHandler().Handle(new MarkThreadReadCommand(_workspaceId, root.Id, 3), CancellationToken.None);
        partway.Data!.UnreadReplies.Should().Be(2);

        var backwards = await MarkHandler().Handle(new MarkThreadReadCommand(_workspaceId, root.Id, 2), CancellationToken.None);
        backwards.Data!.UnreadReplies.Should().Be(2, "a lower sequence changes nothing");

        var beyond = await MarkHandler().Handle(new MarkThreadReadCommand(_workspaceId, root.Id, 99), CancellationToken.None);
        beyond.Data!.UnreadReplies.Should().Be(0, "a sequence above the count reads the thread to its end");
        _followers.Single().ReadReplySequence.Should().Be(5);
    }

    [Fact]
    public async Task A_thread_the_caller_does_not_follow_keeps_no_pointer_and_reports_zero_unread()
    {
        var space = Channel();
        var root = Root(space);
        Thread(space, root, replies: 3);

        var result = await MarkHandler().Handle(new MarkThreadReadCommand(_workspaceId, root.Id, 3), CancellationToken.None);

        result.Data!.UnreadReplies.Should().Be(0);
        _followers.Should().BeEmpty();
    }

    // ---- Replies page (UC-CHT-F03) ---------------------------------------------------------------

    [Fact]
    public async Task A_thread_reads_in_pages_ascending_with_the_root_on_top()
    {
        var space = Channel();
        var root = Root(space);
        Thread(space, root, replies: 5);
        for (var i = 1; i <= 5; i++) Reply(space, root, i, _other, $"reply {i}");

        var first = await RepliesHandler().Handle(new ListThreadRepliesQuery(_workspaceId, root.Id, null, 2), CancellationToken.None);
        first.Data!.Root.Id.Should().Be(root.Id);
        first.Data.Items.Select(m => m.ThreadSequence).Should().Equal(1, 2);
        first.Data.HasNewer.Should().BeTrue();
        first.Data.Thread.ReplyCount.Should().Be(5);

        var second = await RepliesHandler().Handle(new ListThreadRepliesQuery(_workspaceId, root.Id, 2, 2), CancellationToken.None);
        second.Data!.Items.Select(m => m.ThreadSequence).Should().Equal(3, 4);

        var last = await RepliesHandler().Handle(new ListThreadRepliesQuery(_workspaceId, root.Id, 4, 2), CancellationToken.None);
        last.Data!.Items.Select(m => m.ThreadSequence).Should().Equal(5);
        last.Data.HasNewer.Should().BeFalse();
    }

    [Fact]
    public async Task A_deleted_reply_stays_in_the_thread_as_a_tombstone()
    {
        var space = Channel();
        var root = Root(space);
        Thread(space, root, replies: 1);
        var reply = Reply(space, root, 1, _other, "gone soon");
        reply.Delete(_other, Now);

        var result = await RepliesHandler().Handle(new ListThreadRepliesQuery(_workspaceId, root.Id, null, 10), CancellationToken.None);

        var item = result.Data!.Items.Single();
        item.Deleted.Should().BeTrue();
        item.Content.Should().BeNull("the content is gone, the sequence stays");
        item.ThreadSequence.Should().Be(1);
    }

    [Fact]
    public async Task A_private_thread_is_not_found_for_a_non_member()
    {
        var space = Channel(SpaceKind.ChannelPrivate, "secret");
        var root = Root(space);
        Thread(space, root, replies: 1);
        AsUser(_outsider);

        var act = () => RepliesHandler().Handle(new ListThreadRepliesQuery(_workspaceId, root.Id, null, 50), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Asking_for_the_replies_of_a_reply_is_not_found()
    {
        var space = Channel();
        var root = Root(space);
        var reply = Reply(space, root, 1, _other);

        var act = () => RepliesHandler().Handle(new ListThreadRepliesQuery(_workspaceId, reply.Id, null, 50), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---- Threads I follow (UC-CHT-F08) ------------------------------------------------------------

    [Fact]
    public async Task Followed_threads_are_listed_most_recent_first_with_their_unread_counts()
    {
        var space = Channel();
        var older = Root(space);
        var newer = Root(space);
        Thread(space, older, replies: 2, lastReplyAt: Now.AddHours(-5));
        Thread(space, newer, replies: 3, lastReplyAt: Now.AddHours(-1));
        _followers.Add(ThreadFollower.Follow(_accountId, _workspaceId, older.Id, _member, false, readReplySequence: 0));
        _followers.Add(ThreadFollower.Follow(_accountId, _workspaceId, newer.Id, _member, false, readReplySequence: 1));

        var result = await FollowedHandler().Handle(new ListFollowedThreadsQuery(_workspaceId, null, 50), CancellationToken.None);

        result.Data!.Items.Select(i => i.Root.Id).Should().Equal(newer.Id, older.Id);
        result.Data.Items[0].Thread.UnreadReplies.Should().Be(2);
        result.Data.Items[1].Thread.UnreadReplies.Should().Be(2);
        result.Data.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task A_thread_in_a_space_the_caller_has_left_is_not_listed()
    {
        var space = Channel();
        var root = Root(space);
        Thread(space, root, replies: 1);
        _followers.Add(ThreadFollower.Follow(_accountId, _workspaceId, root.Id, _member, false));
        space.RemoveMember(_member, Now);

        var result = await FollowedHandler().Handle(new ListFollowedThreadsQuery(_workspaceId, null, 50), CancellationToken.None);

        result.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Followed_threads_page_with_a_cursor()
    {
        var space = Channel();
        var roots = Enumerable.Range(0, 3).Select(_ => Root(space)).ToList();
        for (var i = 0; i < roots.Count; i++)
        {
            Thread(space, roots[i], replies: 1, lastReplyAt: Now.AddHours(-i - 1));
            _followers.Add(ThreadFollower.Follow(_accountId, _workspaceId, roots[i].Id, _member, false));
        }

        var first = await FollowedHandler().Handle(new ListFollowedThreadsQuery(_workspaceId, null, 2), CancellationToken.None);
        first.Data!.Items.Should().HaveCount(2);
        first.Data.NextCursor.Should().NotBeNull();

        var second = await FollowedHandler().Handle(new ListFollowedThreadsQuery(_workspaceId, first.Data.NextCursor, 2), CancellationToken.None);
        second.Data!.Items.Should().ContainSingle().Which.Root.Id.Should().Be(roots[2].Id);
        second.Data.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task A_malformed_cursor_is_refused()
    {
        var act = () => FollowedHandler().Handle(new ListFollowedThreadsQuery(_workspaceId, "not-a-cursor", 50), CancellationToken.None);

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ValidationException>();
    }

    // ---- Channel history keeps thread replies out of the main flow -----------------------------

    [Fact]
    public async Task Channel_history_hides_thread_replies_unless_they_were_also_sent_to_the_channel()
    {
        var space = Channel();
        var root = Root(space);
        Thread(space, root, replies: 2);
        Reply(space, root, 1, _other, "thread only");
        var alsoSent = Message.CreateThreadReply(_accountId, _workspaceId, space.Id, ++_lastSequence, root.Id, 2, _other,
            "also in channel", "also-1", alsoSentToChannel: true, Now);
        _messages.Add(alsoSent);

        var page = await ListMessagesHandler().Handle(new ListMessagesQuery(_workspaceId, space.Id, null, null, null, 50), CancellationToken.None);

        page.Data!.Items.Select(m => m.Id).Should().BeEquivalentTo(new[] { root.Id, alsoSent.Id });
    }

    private sealed class FakeThreadReadStore(List<ThreadFollower> followers) : IChatThreadReadStore
    {
        public Task<bool> AdvanceAsync(Guid workspaceId, Guid rootMessageId, Guid userId, long target, CancellationToken cancellationToken)
        {
            var follower = followers.SingleOrDefault(f => f.RootMessageId == rootMessageId && f.UserId == userId && f.WorkspaceId == workspaceId);
            if (follower is null) return Task.FromResult(false);
            follower.MarkReadThrough(target);
            return Task.FromResult(true);
        }
    }
}
