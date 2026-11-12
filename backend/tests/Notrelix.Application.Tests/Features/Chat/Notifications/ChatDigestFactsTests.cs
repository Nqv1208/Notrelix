using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Notifications.Services;
using Notrelix.Application.Features.Chat.Public.Digest;
using Notrelix.Application.Features.Collaboration.Public.Notifications;
using Notrelix.Application.Tests.Features.Collaboration;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Tests.Features.Chat.Notifications;

/// <summary>
/// UC-CHT-H09: Chat's answer to "which unread chat notifications still belong in the digest of this person", decided at send time: still a
/// member, not muted, the level allows the reason, not read in Chat, the message still exists, the author never, a thread reply needs a followed
/// unmuted thread. The excerpt is the current text and the path opens the space.
/// </summary>
public class ChatDigestFactsTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 13, 9, 0, 0, TimeSpan.Zero);

    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _author = Guid.NewGuid();
    private readonly Guid _ana = Guid.NewGuid();

    private readonly List<Space> _spaces = [];
    private readonly List<Message> _messages = [];
    private readonly List<ThreadFollower> _followers = [];
    private readonly Mock<IChatDbContext> _db = new();

    public ChatDigestFactsTests()
    {
        _db.Setup(d => d.ChatSpaces).Returns(() => TestDbSet.Create(_spaces).Object);
        _db.Setup(d => d.ChatMemberships).Returns(() => TestDbSet.Create(_spaces.SelectMany(s => s.Memberships).ToList()).Object);
        _db.Setup(d => d.ChatMessages).Returns(() => TestDbSet.Create(_messages).Object);
        _db.Setup(d => d.ChatThreadFollowers).Returns(() => TestDbSet.Create(_followers).Object);
    }

    private Space Channel(string name = "general")
    {
        var space = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, name, null, null, 1, _author, Now.AddDays(-2));
        space.AddMember(_ana, MembershipRole.Member, Now.AddDays(-2));
        _spaces.Add(space);
        return space;
    }

    private Message Send(Space space, string content = "ping @Ana", Guid? author = null)
    {
        var message = Message.CreateUserMessage(_accountId, _workspaceId, space.Id, _messages.Count + 1, author ?? _author, content, Guid.NewGuid().ToString(), null, Now.AddHours(-3));
        _messages.Add(message);
        return message;
    }

    private static ChatDigestCandidate Candidate(Message m, string reason = ChatNotificationReasons.Mention) =>
        new(Guid.NewGuid(), m.SpaceId, m.Id, reason, Now.AddHours(-3));

    private Task<IReadOnlyList<ChatDigestFact>> Facts(params ChatDigestCandidate[] candidates) =>
        new ChatDigestFacts(_db.Object).DeliverableAsync(_workspaceId, _ana, candidates, Now, CancellationToken.None);

    private Membership AnaIn(Space space) => space.Memberships.Single(m => m.UserId == _ana);

    [Fact]
    public async Task AnUnreadMentionInASpaceThePersonIsIn_IsDeliverableWithItsCurrentTextAndPath()
    {
        var channel = Channel();
        var message = Send(channel, "ping @Ana about the release");

        var facts = await Facts(Candidate(message));

        var fact = facts.Should().ContainSingle().Subject;
        fact.SpaceName.Should().Be("general");
        fact.Preview.Should().Be("ping @Ana about the release");
        fact.Path.Should().Be($"/workspaces/{_workspaceId}/chat/{channel.Id}");
        fact.MessageId.Should().Be(message.Id);
    }

    [Fact]
    public async Task AnEditedMessageShowsItsCurrentText()
    {
        var channel = Channel();
        var message = Send(channel, "old text");
        message.Edit("new text", _author, Now.AddHours(-2));

        (await Facts(Candidate(message))).Single().Preview.Should().Be("new text");
    }

    [Fact]
    public async Task ALongMessageIsCutToThePreviewBound()
    {
        var channel = Channel();
        var message = Send(channel, new string('x', IChatDigestFacts.PreviewMaxLength + 100));

        (await Facts(Candidate(message))).Single().Preview!.Length.Should().Be(IChatDigestFacts.PreviewMaxLength);
    }

    [Fact]
    public async Task AMutedChannelAndALevelNoneGiveNothing_AnExpiredMuteDoesNot()
    {
        var channel = Channel();
        var message = Send(channel);
        AnaIn(channel).SetNotificationLevel(NotificationLevel.None, null, Now.AddHours(-4));
        (await Facts(Candidate(message))).Should().BeEmpty();

        AnaIn(channel).SetNotificationLevel(NotificationLevel.None, Now.AddHours(-1), Now.AddHours(-4));
        (await Facts(Candidate(message))).Should().ContainSingle("the mute ended before the digest is sent");
    }

    [Fact]
    public async Task AMentionsLevelKeepsMentionsButDropsAConversationMessage()
    {
        var channel = Channel();
        var message = Send(channel);
        AnaIn(channel).SetNotificationLevel(NotificationLevel.Mentions, null, Now.AddHours(-4));

        (await Facts(Candidate(message, ChatNotificationReasons.Mention))).Should().ContainSingle();
        (await Facts(Candidate(message, ChatNotificationReasons.DirectMessage))).Should().BeEmpty();
    }

    [Fact]
    public async Task APersonWhoLeftOrWasRemovedGetsNothing()
    {
        var channel = Channel();
        var message = Send(channel);
        channel.RemoveMember(_ana, Now.AddHours(-1));

        (await Facts(Candidate(message))).Should().BeEmpty();
    }

    [Fact]
    public async Task ADeletedSpaceGivesNothing()
    {
        var channel = Channel();
        var message = Send(channel);
        channel.Delete(_author, Now.AddHours(-1));

        (await Facts(Candidate(message))).Should().BeEmpty();
    }

    [Fact]
    public async Task AMessageThePersonHasReadInChatIsNotMissed()
    {
        var channel = Channel();
        var read = Send(channel);
        var unread = Send(channel);
        AnaIn(channel).AdvanceReadSequence(read.Sequence);

        var facts = await Facts(Candidate(read), Candidate(unread));

        facts.Should().ContainSingle().Which.MessageId.Should().Be(unread.Id);
    }

    [Fact]
    public async Task ADeletedMessageAndTheAuthorsOwnMessageAreNeverListed()
    {
        var channel = Channel();
        var deleted = Send(channel);
        deleted.Delete(_author, Now.AddHours(-2));
        var own = Send(channel, "my own", _ana);

        (await Facts(Candidate(deleted), Candidate(own))).Should().BeEmpty();
    }

    [Fact]
    public async Task ACandidateWhoseMessageIsInAnotherSpaceIsDropped()
    {
        var channel = Channel();
        var other = Channel("other");
        var message = Send(other);

        (await Facts(new ChatDigestCandidate(Guid.NewGuid(), channel.Id, message.Id, ChatNotificationReasons.Mention, Now))).Should().BeEmpty();
    }

    [Fact]
    public async Task AThreadReplyNeedsAFollowedUnmutedThread_AndOneAlreadyReadIsNotMissed()
    {
        var channel = Channel();
        var root = Send(channel, "root");
        var reply = Message.CreateThreadReply(_accountId, _workspaceId, channel.Id, 2, root.Id, 1, _author, "a reply", "c-reply", false, Now.AddHours(-2));
        _messages.Add(reply);

        (await Facts(Candidate(reply, ChatNotificationReasons.ThreadReply))).Should().BeEmpty("the person does not follow the thread");

        var follower = ThreadFollower.Follow(_accountId, _workspaceId, root.Id, _ana, false);
        _followers.Add(follower);
        (await Facts(Candidate(reply, ChatNotificationReasons.ThreadReply))).Should().ContainSingle();

        follower.SetMuted(true);
        (await Facts(Candidate(reply, ChatNotificationReasons.ThreadReply))).Should().BeEmpty("a muted thread gives nothing");

        follower.SetMuted(false);
        follower.MarkReadThrough(1);
        (await Facts(Candidate(reply, ChatNotificationReasons.ThreadReply))).Should().BeEmpty("the reply was read in the thread");
    }

    [Fact]
    public async Task AConversationHasNoNameAndItsOwnPath()
    {
        var direct = Space.CreateDirect(_accountId, _workspaceId, _author, _ana, Now.AddDays(-1));
        _spaces.Add(direct);
        var message = Send(direct, "hello");

        var fact = (await Facts(Candidate(message, ChatNotificationReasons.DirectMessage))).Single();

        fact.SpaceName.Should().BeNull();
        fact.Path.Should().Be($"/workspaces/{_workspaceId}/chat/dm/{direct.Id}");
    }

    [Fact]
    public async Task NoCandidatesReadNothing()
    {
        (await Facts()).Should().BeEmpty();

        _db.VerifyGet(d => d.ChatMessages, Times.Never);
    }
}
