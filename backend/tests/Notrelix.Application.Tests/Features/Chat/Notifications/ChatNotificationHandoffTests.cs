using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Notifications.Services;
using Notrelix.Application.Features.Collaboration.Public.Notifications;
using Notrelix.Application.Tests.Features.Collaboration;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;
using Microsoft.Extensions.Logging.Abstractions;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Tests.Features.Chat.Notifications;

/// <summary>
/// UC-CHT-G09: the Chat side maps a message fact to notification intents at delivery. Each reason, a recipient who left before delivery, a
/// muted space, the author never notified, one reason per recipient, the broadcast bound and no intent for a deleted, system or archived
/// message are decided here; Collaboration's own rules (preferences, duplicates) are covered where it is implemented.
/// </summary>
public class ChatNotificationHandoffTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _author = Guid.NewGuid();
    private readonly Guid _ana = Guid.NewGuid();
    private readonly Guid _bao = Guid.NewGuid();
    private readonly Guid _chi = Guid.NewGuid();

    private readonly List<Space> _spaces = [];
    private readonly List<Message> _messages = [];
    private readonly List<MessageMention> _mentions = [];
    private readonly List<ThreadFollower> _followers = [];
    private readonly RecordingIntents _intents = new();
    private readonly Mock<IChatDbContext> _db = new();

    public ChatNotificationHandoffTests()
    {
        _db.Setup(d => d.ChatSpaces).Returns(() => TestDbSet.Create(_spaces).Object);
        _db.Setup(d => d.ChatMemberships).Returns(() => TestDbSet.Create(_spaces.SelectMany(s => s.Memberships).ToList()).Object);
        _db.Setup(d => d.ChatMessages).Returns(() => TestDbSet.Create(_messages).Object);
        _db.Setup(d => d.ChatMessageMentions).Returns(() => TestDbSet.Create(_mentions).Object);
        _db.Setup(d => d.ChatThreadFollowers).Returns(() => TestDbSet.Create(_followers).Object);
    }

    private ChatNotificationHandoff Handoff() => new(_db.Object, _intents, NullLogger<ChatNotificationHandoff>.Instance);

    private Space Channel(params Guid[] members)
    {
        var space = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "general", null, null, 1, _author, Now);
        foreach (var member in members)
            space.AddMember(member, MembershipRole.Member, Now);
        _spaces.Add(space);
        return space;
    }

    private Message Send(Space space, string content = "hello team", Guid? author = null)
    {
        var message = Message.CreateUserMessage(_accountId, _workspaceId, space.Id, _messages.Count + 1, author ?? _author, content, Guid.NewGuid().ToString(), null, Now);
        _messages.Add(message);
        return message;
    }

    private void Mention(Space space, Message message, MentionKind kind, Guid? target = null) =>
        _mentions.Add(MessageMention.Create(_accountId, _workspaceId, space.Id, message.Id, new MentionSpan(kind, 0, 5, target)));

    private Task<ChatNotificationDelivery> Run(Message message) =>
        Handoff().HandOffAsync(_workspaceId, message.Id, Guid.NewGuid(), Now, CancellationToken.None);

    private static Membership MembershipOf(Space space, Guid userId) => space.Memberships.Single(m => m.UserId == userId);

    [Fact]
    public async Task A_mention_of_a_member_hands_a_mention_intent_with_the_source()
    {
        var channel = Channel(_ana, _bao);
        var message = Send(channel, "hi @Ana");
        Mention(channel, message, MentionKind.User, _ana);

        await Run(message);

        _intents.Intents.Should().ContainSingle().Which.Should().Be(new ChatNotificationIntent(_ana, ChatNotificationReasons.Mention));
        _intents.Source!.MessageId.Should().Be(message.Id);
        _intents.Source.SpaceId.Should().Be(channel.Id);
        _intents.Source.ActorUserId.Should().Be(_author);
        _intents.Source.SpaceName.Should().Be("general");
        _intents.Source.Preview.Should().Be("hi @Ana");
    }

    [Fact]
    public async Task A_message_in_a_direct_conversation_notifies_the_other_person_only()
    {
        var direct = Space.CreateDirect(_accountId, _workspaceId, _author, _ana, Now);
        _spaces.Add(direct);
        var message = Send(direct);

        await Run(message);

        _intents.Intents.Should().ContainSingle().Which.Should().Be(new ChatNotificationIntent(_ana, ChatNotificationReasons.DirectMessage));
        _intents.Source!.SpaceName.Should().BeNull("a conversation has no name");
    }

    [Fact]
    public async Task A_message_in_a_group_conversation_notifies_every_other_participant()
    {
        var group = Space.CreateGroup(_accountId, _workspaceId, null, [_ana, _bao], _author, Now);
        _spaces.Add(group);

        await Run(Send(group));

        _intents.Intents.Select(i => i.RecipientUserId).Should().BeEquivalentTo([_ana, _bao]);
        _intents.Intents.Should().OnlyContain(i => i.Reason == ChatNotificationReasons.DirectMessage);
    }

    [Fact]
    public async Task A_plain_channel_message_notifies_nobody()
    {
        var channel = Channel(_ana, _bao);

        await Run(Send(channel));

        _intents.Calls.Should().Be(0);
    }

    [Fact]
    public async Task A_thread_reply_notifies_the_followers_who_are_not_muted_and_not_the_author()
    {
        var channel = Channel(_ana, _bao, _chi);
        var root = Send(channel, "root", _ana);
        var reply = Message.CreateThreadReply(_accountId, _workspaceId, channel.Id, 2, root.Id, 1, _author, "a reply", Guid.NewGuid().ToString(), false, Now);
        _messages.Add(reply);
        _followers.Add(ThreadFollower.Follow(_accountId, _workspaceId, root.Id, _ana, muted: false));
        _followers.Add(ThreadFollower.Follow(_accountId, _workspaceId, root.Id, _bao, muted: true));
        _followers.Add(ThreadFollower.Follow(_accountId, _workspaceId, root.Id, _author, muted: false));

        await Run(reply);

        _intents.Intents.Should().ContainSingle().Which.Should().Be(new ChatNotificationIntent(_ana, ChatNotificationReasons.ThreadReply));
        _intents.Source!.ThreadRootId.Should().Be(root.Id);
    }

    [Fact]
    public async Task A_channel_broadcast_notifies_every_other_member_and_a_here_broadcast_notifies_nobody_without_presence()
    {
        var channel = Channel(_ana, _bao);
        var everyone = Send(channel, "@channel go");
        Mention(channel, everyone, MentionKind.BroadcastChannel);
        var here = Send(channel, "@here go");
        Mention(channel, here, MentionKind.BroadcastHere);

        await Run(everyone);
        _intents.Intents.Select(i => i.RecipientUserId).Should().BeEquivalentTo([_ana, _bao]);
        _intents.Intents.Should().OnlyContain(i => i.Reason == ChatNotificationReasons.Broadcast);

        _intents.Reset();
        await Run(here);
        _intents.Calls.Should().Be(0, "@here reaches present members and presence is not a fact this side holds");
    }

    [Fact]
    public async Task A_person_who_left_the_channel_before_delivery_is_not_notified()
    {
        var channel = Channel(_ana, _bao);
        var message = Send(channel, "@Ana @Bao");
        Mention(channel, message, MentionKind.User, _ana);
        Mention(channel, message, MentionKind.User, _bao);
        channel.RemoveMember(_ana, Now.AddMinutes(1));

        await Run(message);

        _intents.Intents.Should().ContainSingle().Which.RecipientUserId.Should().Be(_bao);
    }

    [Fact]
    public async Task A_person_who_never_joined_the_channel_is_not_notified_of_a_mention()
    {
        var channel = Channel(_bao);
        var message = Send(channel, "@Ana");
        Mention(channel, message, MentionKind.User, _ana);

        await Run(message);

        _intents.Calls.Should().Be(0);
    }

    [Fact]
    public async Task A_muted_space_gives_nothing_for_any_reason_and_an_elapsed_mute_does_not_count()
    {
        var channel = Channel(_ana, _bao);
        var message = Send(channel, "@channel @Ana @Bao");
        Mention(channel, message, MentionKind.BroadcastChannel);
        Mention(channel, message, MentionKind.User, _ana);
        Mention(channel, message, MentionKind.User, _bao);
        MembershipOf(channel, _ana).SetNotificationLevel(NotificationLevel.None, null, Now);
        MembershipOf(channel, _bao).SetNotificationLevel(NotificationLevel.None, Now.AddDays(-1).AddSeconds(1), Now.AddDays(-2));

        await Run(message);

        _intents.Intents.Should().ContainSingle("the mute of Bao ended yesterday").Which.RecipientUserId.Should().Be(_bao);
    }

    [Fact]
    public async Task The_mentions_level_gives_mentions_and_broadcasts_but_not_plain_conversation_messages()
    {
        var group = Space.CreateGroup(_accountId, _workspaceId, null, [_ana, _bao], _author, Now);
        _spaces.Add(group);
        MembershipOf(group, _ana).SetNotificationLevel(NotificationLevel.Mentions, null, Now);
        var plain = Send(group);
        await Run(plain);
        _intents.Intents.Should().ContainSingle().Which.RecipientUserId.Should().Be(_bao);

        _intents.Reset();
        var mention = Send(group, "@Ana");
        Mention(group, mention, MentionKind.User, _ana);
        await Run(mention);
        _intents.Intents.Should().Contain(new ChatNotificationIntent(_ana, ChatNotificationReasons.Mention));
    }

    [Fact]
    public async Task The_author_is_never_a_recipient_even_when_they_mention_themselves_or_broadcast()
    {
        var channel = Channel(_ana);
        var message = Send(channel, "@me @channel");
        Mention(channel, message, MentionKind.User, _author);
        Mention(channel, message, MentionKind.BroadcastChannel);

        await Run(message);

        _intents.Intents.Select(i => i.RecipientUserId).Should().BeEquivalentTo([_ana]);
    }

    [Fact]
    public async Task One_recipient_gets_one_reason_the_most_specific_one()
    {
        var channel = Channel(_ana, _bao);
        var message = Send(channel, "@channel @Ana");
        Mention(channel, message, MentionKind.BroadcastChannel);
        Mention(channel, message, MentionKind.User, _ana);

        await Run(message);

        _intents.Intents.Should().HaveCount(2);
        _intents.Intents.Single(i => i.RecipientUserId == _ana).Reason.Should().Be(ChatNotificationReasons.Mention);
        _intents.Intents.Single(i => i.RecipientUserId == _bao).Reason.Should().Be(ChatNotificationReasons.Broadcast);
    }

    [Fact]
    public async Task A_broadcast_is_bounded_by_the_fan_out_limit()
    {
        var members = Enumerable.Range(0, ChatNotificationHandoff.MaxRecipients + 25).Select(_ => Guid.NewGuid()).ToArray();
        var channel = Channel(members);
        var message = Send(channel, "@channel");
        Mention(channel, message, MentionKind.BroadcastChannel);

        await Run(message);

        _intents.Intents.Should().HaveCount(ChatNotificationHandoff.MaxRecipients);
        _intents.Intents.Select(i => i.RecipientUserId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task A_deleted_message_a_system_message_and_an_archived_channel_give_nothing()
    {
        var channel = Channel(_ana);
        var deleted = Send(channel, "@Ana");
        Mention(channel, deleted, MentionKind.User, _ana);
        deleted.Delete(_author, Now);
        await Run(deleted);

        var system = Message.CreateSystemMessage(_accountId, _workspaceId, channel.Id, 9, _author, "joined", Now);
        _messages.Add(system);
        Mention(channel, system, MentionKind.User, _ana);
        await Run(system);

        var live = Send(channel, "@Ana");
        Mention(channel, live, MentionKind.User, _ana);
        channel.SetArchived(true, _author, Now);
        await Run(live);

        _intents.Calls.Should().Be(0);
    }

    [Fact]
    public async Task A_message_that_is_gone_gives_nothing()
    {
        var result = await Handoff().HandOffAsync(_workspaceId, Guid.NewGuid(), null, Now, CancellationToken.None);

        result.Should().Be(ChatNotificationDelivery.None);
        _intents.Calls.Should().Be(0);
    }

    [Theory]
    [InlineData(NotificationLevel.All, true, false, false, false, ChatNotificationReasons.Mention)]
    [InlineData(NotificationLevel.None, true, true, true, true, null)]
    [InlineData(NotificationLevel.Mentions, false, true, false, false, ChatNotificationReasons.ThreadReply)]
    [InlineData(NotificationLevel.Mentions, false, false, true, false, null)]
    [InlineData(NotificationLevel.Mentions, false, false, true, true, ChatNotificationReasons.Broadcast)]
    [InlineData(NotificationLevel.All, false, false, true, false, ChatNotificationReasons.DirectMessage)]
    [InlineData(NotificationLevel.All, false, false, false, false, null)]
    public void ReasonFor_picks_one_reason_by_level(NotificationLevel level, bool mentioned, bool follows, bool conversation, bool broadcast, string? expected) =>
        ChatNotificationHandoff.ReasonFor(level, mentioned, follows, conversation, broadcast).Should().Be(expected);

    private sealed class RecordingIntents : IChatNotificationIntents
    {
        public int Calls { get; private set; }
        public ChatNotificationSource? Source { get; private set; }
        public List<ChatNotificationIntent> Intents { get; } = [];

        public void Reset()
        {
            Calls = 0;
            Source = null;
            Intents.Clear();
        }

        public Task<ChatNotificationDelivery> DeliverAsync(
            ChatNotificationSource source, IReadOnlyCollection<ChatNotificationIntent> intents, CancellationToken cancellationToken)
        {
            Calls++;
            Source = source;
            Intents.AddRange(intents);
            return Task.FromResult(new ChatNotificationDelivery(intents.Count, 0, 0));
        }
    }
}
