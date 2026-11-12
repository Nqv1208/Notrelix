using System.Text.RegularExpressions;
using Notrelix.Domain.Governance.Permissions;
using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Audit.Services;
using Notrelix.Application.Features.Chat.Blocks.Commands.SetPersonBlock;
using Notrelix.Application.Features.Chat.Channels.Queries.AuthorizeChannelRealtimeSubscription;
using Notrelix.Application.Features.Chat.Channels.Queries.GetChannel;
using Notrelix.Application.Features.Chat.Channels.Queries.ListChannels;
using Notrelix.Application.Features.Chat.Conversations.Commands.StartConversation;
using Notrelix.Application.Features.Chat.Members.Commands.JoinChannel;
using Notrelix.Application.Features.Chat.Members.Commands.RemoveChannelMember;
using Notrelix.Application.Features.Chat.Mentions.Queries.ListMentionables;
using Notrelix.Application.Features.Chat.Messages.Queries.ListMessages;
using Notrelix.Application.Features.Chat.Messages.Services;
using Notrelix.Application.Features.Chat.Pins.Queries.ListPins;
using Notrelix.Application.Features.Chat.Reactions.Queries.ListReactors;
using Notrelix.Application.Features.Chat.ReadState.Queries.GetUnreadSummary;
using Notrelix.Application.Features.Chat.Threads.Queries.ListFollowedThreads;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Application.Common.Auditing;
using Notrelix.Application.Tests.Features.Collaboration;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Testing.Application.Fakes;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Tests.Features.Chat.Guests;

/// <summary>
/// UC-CHT-H02: a Guest sees only the spaces they were added to (CHT-004). Each case asks one read or write path of Chat as a Guest and
/// proves it answers the same NotFound for a space they were not added to as for a missing one, lists nothing of the Workspace's
/// directory, and stops answering when they are removed or deactivated.
/// </summary>
public class GuestAccessHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 9, 0, 0, TimeSpan.Zero);

    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private readonly Guid _colleague = Guid.NewGuid();
    private readonly Guid _guest = Guid.NewGuid();
    private readonly Guid _otherGuest = Guid.NewGuid();

    private readonly Mock<ICurrentRequestContext> _context = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IWorkspaceMemberFacts> _members = new();
    private readonly Mock<IAccessChangeSignal> _access = new();
    private readonly Mock<IDirectConversationLock> _pairLock = new();
    private readonly Mock<IChatReadStateStore> _readStates = new();
    private readonly List<Space> _spaces = [];
    private readonly List<Message> _messages = [];
    private readonly List<MessageMention> _mentions = [];
    private readonly List<Reaction> _reactions = [];
    private readonly List<MessageThread> _threads = [];
    private readonly List<ThreadFollower> _followers = [];
    private readonly List<DirectPair> _pairs = [];
    private readonly List<PersonBlock> _blocks = [];
    private readonly List<Pin> _pins = [];
    private readonly Mock<IChatDbContext> _db = new();
    private readonly Mock<ISpaceSequenceAllocator> _sequences = new();
    private readonly Dictionary<Guid, WorkspaceMemberFact> _facts = [];

    /// <summary>A public channel the Guest was not added to.</summary>
    private readonly Space _general;

    /// <summary>A private channel the Guest was not added to.</summary>
    private readonly Space _secret;

    /// <summary>A public channel the Guest was added to.</summary>
    private readonly Space _shared;

    public GuestAccessHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(() => Now);
        _context.Setup(c => c.RequireAccountId()).Returns(_accountId);
        _context.Setup(c => c.RequireWorkspaceId()).Returns(_workspaceId);
        _context.Setup(c => c.IsAuthenticated).Returns(true);

        _db.Setup(d => d.ChatSpaces).Returns(() => TestDbSet.Create(_spaces).Object);
        _db.Setup(d => d.ChatMemberships).Returns(() => TestDbSet.Create(_spaces.SelectMany(s => s.Memberships).ToList()).Object);
        _db.Setup(d => d.ChatPermissionOverrides).Returns(() => TestDbSet.Create(_spaces.SelectMany(s => s.PermissionOverrides).ToList()).Object);
        _db.Setup(d => d.ChatChannelGroups).Returns(TestDbSet.Create(new List<ChannelGroup>()).Object);
        _db.Setup(d => d.ChatMessages).Returns(() => TestDbSet.Create(_messages).Object);
        _db.Setup(d => d.ChatReactions).Returns(() => TestDbSet.Create(_reactions).Object);
        _db.Setup(d => d.ChatMessageMentions).Returns(() => TestDbSet.Create(_mentions).Object);
        _db.Setup(d => d.ChatThreads).Returns(() => TestDbSet.Create(_threads).Object);
        _db.Setup(d => d.ChatThreadFollowers).Returns(() => TestDbSet.Create(_followers).Object);
        _db.Setup(d => d.ChatDirectPairs).Returns(() => TestDbSet.Create(_pairs).Object);
        _db.Setup(d => d.ChatBlocks).Returns(() => TestDbSet.Create(_blocks).Object);
        _db.Setup(d => d.ChatPins).Returns(() => TestDbSet.Create(_pins).Object);
        _sequences.Setup(s => s.LockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _members.Setup(m => m.FindAsync(_workspaceId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, Guid user, CancellationToken _) => _facts.GetValueOrDefault(user));
        _members.Setup(m => m.ListActiveColleagueIdsAsync(_workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([_owner, _member, _colleague]);
        _facts[_owner] = new WorkspaceMemberFact(_owner, "Owner", true);
        _facts[_member] = new WorkspaceMemberFact(_member, "Member", true);
        _facts[_colleague] = new WorkspaceMemberFact(_colleague, "Member", true);
        _facts[_guest] = new WorkspaceMemberFact(_guest, "Guest", true);
        _facts[_otherGuest] = new WorkspaceMemberFact(_otherGuest, "Guest", true);

        _general = Channel("general", SpaceKind.ChannelPublic);
        _general.AddMember(_member, MembershipRole.Member, Now);
        _general.AddMember(_colleague, MembershipRole.Member, Now);
        _secret = Channel("secret", SpaceKind.ChannelPrivate);
        _shared = Channel("shared", SpaceKind.ChannelPublic);
        _shared.AddMember(_member, MembershipRole.Member, Now);
        _shared.AddMember(_guest, MembershipRole.Member, Now);

        AsUser(_guest);
    }

    private void AsUser(Guid userId) => _context.Setup(c => c.UserId).Returns(userId);

    private Space Channel(string name, SpaceKind kind)
    {
        var space = Space.CreateChannel(_accountId, _workspaceId, kind, name, null, null, 1, _owner, Now);
        _spaces.Add(space);
        return space;
    }

    private Message Post(Space space, long sequence, Guid author, string content = "hello")
    {
        var message = Message.CreateUserMessage(_accountId, _workspaceId, space.Id, sequence, author, content, $"c-{sequence}-{Guid.NewGuid():N}", null, Now.AddHours(-1));
        _messages.Add(message);
        return message;
    }

    private ListChannelsQueryHandler ListChannels() => new(_db.Object, _context.Object, _members.Object);
    private GetChannelQueryHandler GetChannel() => new(_db.Object, _context.Object, _members.Object);

    private static async Task<NotFoundException> NotFound(Func<Task> call) =>
        (await FluentActions.Awaiting(call).Should().ThrowAsync<NotFoundException>()).Which;

    // ---- Channel list and browser -----------------------------------------------------------------

    [Fact]
    public async Task The_sidebar_of_a_guest_lists_only_the_channels_they_were_added_to()
    {
        var result = await ListChannels().Handle(new ListChannelsQuery(_workspaceId), CancellationToken.None);

        result.Data!.Select(c => c.Id).Should().Equal(_shared.Id);
        result.Data.Should().OnlyContain(c => !c.Capabilities.CanModerate && !c.Capabilities.CanBroadcast && !c.Capabilities.CanInvite && !c.Capabilities.CanPin);
    }

    [Fact]
    public async Task The_sidebar_of_a_guest_added_nowhere_is_empty()
    {
        AsUser(_otherGuest);

        var result = await ListChannels().Handle(new ListChannelsQuery(_workspaceId), CancellationToken.None);

        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task The_channel_browser_of_a_guest_is_empty_even_though_the_workspace_has_public_channels()
    {
        var result = await ListChannels().Handle(new ListChannelsQuery(_workspaceId, Browse: true), CancellationToken.None);

        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task A_public_channel_the_guest_was_not_added_to_is_not_found_and_not_forbidden()
    {
        await NotFound(() => GetChannel().Handle(new GetChannelQuery(_workspaceId, _general.Id), CancellationToken.None));
        await NotFound(() => GetChannel().Handle(new GetChannelQuery(_workspaceId, _secret.Id), CancellationToken.None));
        await NotFound(() => GetChannel().Handle(new GetChannelQuery(_workspaceId, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task The_member_list_of_an_added_channel_holds_the_people_of_that_channel_only()
    {
        var result = await GetChannel().Handle(new GetChannelQuery(_workspaceId, _shared.Id), CancellationToken.None);

        result.Data!.Members.Select(m => m.UserId).Should().BeEquivalentTo([_owner, _member, _guest]);
        result.Data.Members.Select(m => m.UserId).Should().NotContain([_colleague, _otherGuest]);
    }

    // ---- Mentionables, conversations, blocks ------------------------------------------------------

    [Fact]
    public async Task The_mention_picker_of_a_guest_lists_the_people_of_the_space_and_no_directory()
    {
        var handler = new ListMentionablesQueryHandler(_db.Object, _context.Object, _members.Object, Mock.Of<Notrelix.Application.Features.Chat.Mentions.Services.IBroadcastAuthorizer>(b => b.CanBroadcastAsync(_workspaceId, It.IsAny<CancellationToken>()) == Task.FromResult(true)));

        var result = await handler.Handle(new ListMentionablesQuery(_workspaceId, _shared.Id), CancellationToken.None);

        result.Data!.People.Select(p => p.UserId).Should().BeEquivalentTo([_owner, _member, _guest]);
        result.Data.People.Should().OnlyContain(p => p.InSpace);
        result.Data.CanBroadcast.Should().BeFalse();
        await NotFound(() => handler.Handle(new ListMentionablesQuery(_workspaceId, _general.Id), CancellationToken.None));
    }

    private StartConversationCommandHandler StartHandler() => new(_db.Object, _context.Object, _members.Object, _pairLock.Object, _clock.Object);

    [Fact]
    public async Task A_guest_starts_a_direct_conversation_only_with_a_person_they_share_a_space_with()
    {
        await NotFound(() => StartHandler().Handle(new StartConversationCommand(_workspaceId, "direct", _colleague, null, null), CancellationToken.None));
        await NotFound(() => StartHandler().Handle(new StartConversationCommand(_workspaceId, "direct", _otherGuest, null, null), CancellationToken.None));

        var ok = await StartHandler().Handle(new StartConversationCommand(_workspaceId, "direct", _member, null, null), CancellationToken.None);
        ok.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task A_group_of_a_guest_skips_the_people_they_cannot_reach_without_telling_why()
    {
        var result = await StartHandler().Handle(
            new StartConversationCommand(_workspaceId, "group", null, [_member, _owner, _colleague, _otherGuest], "ours"), CancellationToken.None);

        result.Data!.Skipped.Should().BeEquivalentTo([_colleague, _otherGuest]);
    }

    private SetPersonBlockCommandHandler BlockHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object);

    [Fact]
    public async Task A_guest_cannot_use_a_block_to_learn_who_is_in_the_workspace()
    {
        // LEAK FIXED: a block of a person who exists was a success and of one who does not was a NotFound.
        var stranger = await NotFound(() => BlockHandler().Handle(new SetPersonBlockCommand(_workspaceId, _colleague, true), CancellationToken.None));
        var missing = await NotFound(() => BlockHandler().Handle(new SetPersonBlockCommand(_workspaceId, Guid.NewGuid(), true), CancellationToken.None));
        Regex.Replace(stranger.Message, "[0-9a-f-]{36}", "id").Should().Be(Regex.Replace(missing.Message, "[0-9a-f-]{36}", "id"), "a person who is there and one who is not read the same");
        missing.Should().BeOfType(stranger.GetType());

        var shared = await BlockHandler().Handle(new SetPersonBlockCommand(_workspaceId, _member, true), CancellationToken.None);
        shared.Succeeded.Should().BeTrue();
        _blocks.Should().ContainSingle();
    }

    // ---- Messages, links, pins, reactions, files, threads -----------------------------------------

    private ListMessagesQueryHandler ListMessages() => new(_db.Object, _context.Object, _members.Object, new InMemoryChatMessageAttachments());

    [Fact]
    public async Task The_messages_of_a_space_the_guest_was_not_added_to_are_not_found()
    {
        Post(_general, 1, _member);

        await NotFound(() => ListMessages().Handle(new ListMessagesQuery(_workspaceId, _general.Id, null, null, null, 50), CancellationToken.None));
        await NotFound(() => ListMessages().Handle(new ListMessagesQuery(_workspaceId, _secret.Id, null, null, null, 50), CancellationToken.None));
    }

    [Fact]
    public async Task A_link_to_a_public_channel_the_guest_was_not_added_to_is_left_out_of_what_the_guest_reads_but_not_what_a_member_reads()
    {
        // LEAK FIXED: the link resolved for a Guest as for a Member, so the id of a public channel outside their spaces reached them.
        var message = Post(_shared, 1, _member, "see #general and #shared");
        _mentions.Add(MessageMention.Create(_accountId, _workspaceId, _shared.Id, message.Id, new MentionSpan(MentionKind.ChannelLink, 4, 8, _general.Id)));
        _mentions.Add(MessageMention.Create(_accountId, _workspaceId, _shared.Id, message.Id, new MentionSpan(MentionKind.ChannelLink, 17, 7, _shared.Id)));
        var query = new ListMessagesQuery(_workspaceId, _shared.Id, null, null, null, 50);

        var asGuest = await ListMessages().Handle(query, CancellationToken.None);
        asGuest.Data!.Items.Single().Mentions!.Select(m => m.SpaceId).Should().Equal(_shared.Id);

        AsUser(_member);
        var asMember = await ListMessages().Handle(query, CancellationToken.None);
        asMember.Data!.Items.Single().Mentions!.Select(m => m.SpaceId).Should().BeEquivalentTo([_general.Id, _shared.Id]);
    }

    [Fact]
    public async Task The_pins_and_the_reactors_of_a_space_the_guest_was_not_added_to_are_not_found()
    {
        var message = Post(_general, 1, _member);
        _reactions.Add(Reaction.Create(_accountId, _workspaceId, _general.Id, message.Id, _member, "👍", Now));

        await NotFound(() => new ListPinsQueryHandler(_db.Object, _context.Object, _members.Object, new InMemoryChatMessageAttachments())
            .Handle(new ListPinsQuery(_workspaceId, _general.Id, null, 20), CancellationToken.None));
        await NotFound(() => new ListReactorsQueryHandler(_db.Object, _context.Object, _members.Object)
            .Handle(new ListReactorsQuery(_workspaceId, message.Id, null, null, 20), CancellationToken.None));
        await NotFound(() => new Notrelix.Application.Features.Chat.Attachments.Queries.ListMessageAttachments.ListMessageAttachmentsQueryHandler(
                _db.Object, _context.Object, _members.Object, new InMemoryChatMessageAttachments())
            .Handle(new Notrelix.Application.Features.Chat.Attachments.Queries.ListMessageAttachments.ListMessageAttachmentsQuery(_workspaceId, message.Id), CancellationToken.None));
        await NotFound(() => new Notrelix.Application.Features.Chat.Attachments.Queries.GetMessageAttachmentContent.GetMessageAttachmentContentQueryHandler(
                _db.Object, _context.Object, _members.Object, new InMemoryChatMessageAttachments())
            .Handle(new Notrelix.Application.Features.Chat.Attachments.Queries.GetMessageAttachmentContent.GetMessageAttachmentContentQuery(_workspaceId, message.Id, Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task The_followed_threads_and_the_unread_summary_of_a_guest_hold_only_their_spaces()
    {
        var root = Post(_general, 1, _member);
        _threads.Add(MessageThread.Open(_accountId, _workspaceId, _general.Id, root.Id, Now));
        _followers.Add(ThreadFollower.Follow(_accountId, _workspaceId, root.Id, _guest, muted: false));
        Post(_shared, 1, _member);

        var followed = await new ListFollowedThreadsQueryHandler(_db.Object, _context.Object, _members.Object, _clock.Object, new InMemoryChatMessageAttachments())
            .Handle(new ListFollowedThreadsQuery(_workspaceId, null, 20), CancellationToken.None);
        var unread = await new GetUnreadSummaryQueryHandler(_db.Object, _context.Object, _members.Object, _clock.Object)
            .Handle(new GetUnreadSummaryQuery(_workspaceId), CancellationToken.None);

        followed.Data!.Items.Should().BeEmpty();
        unread.Data!.Spaces.Select(s => s.SpaceId).Should().Equal(_shared.Id);
    }

    [Fact]
    public async Task A_guest_does_not_join_a_channel_they_were_not_added_to()
    {
        var handler = new JoinChannelCommandHandler(_db.Object, _context.Object, _members.Object, _clock.Object,
            new ChatSystemMessages(_db.Object, _sequences.Object, _clock.Object));

        await FluentActions.Awaiting(() => handler.Handle(new JoinChannelCommand(_workspaceId, _general.Id), CancellationToken.None))
            .Should().ThrowAsync<ForbiddenException>();
        _general.IsActiveMember(_guest).Should().BeFalse();
    }

    // ---- Realtime, removal and deactivation -------------------------------------------------------

    private AuthorizeChannelRealtimeSubscriptionQueryHandler Authorize()
    {
        var reader = new Mock<IExecutionContextReader>();
        reader.Setup(r => r.Snapshot).Returns(() => new ExecutionContextSnapshot(
            _context.Object.UserId, _accountId, _workspaceId, null, ApplicationPrincipalKind.Authenticated,
            ApplicationScopeKind.Resource, Guid.NewGuid().ToString("D")));
        return new AuthorizeChannelRealtimeSubscriptionQueryHandler(reader.Object, _db.Object, _members.Object);
    }

    [Fact]
    public async Task A_guest_cannot_subscribe_to_the_stream_of_a_space_they_were_not_added_to()
    {
        var ok = await Authorize().Handle(new AuthorizeChannelRealtimeSubscriptionQuery(_shared.Id), CancellationToken.None);
        ok.Succeeded.Should().BeTrue();

        await NotFound(() => Authorize().Handle(new AuthorizeChannelRealtimeSubscriptionQuery(_general.Id), CancellationToken.None));
        await NotFound(() => Authorize().Handle(new AuthorizeChannelRealtimeSubscriptionQuery(_secret.Id), CancellationToken.None));
    }

    [Fact]
    public async Task When_a_guest_is_removed_from_a_space_the_next_call_is_not_found_and_the_gateway_is_told()
    {
        AsUser(_owner);
        var remove = new RemoveChannelMemberCommandHandler(_db.Object, _context.Object, _members.Object, _clock.Object,
            new ChatSystemMessages(_db.Object, _sequences.Object, _clock.Object), _access.Object,
            new ChatAuditFacts(Mock.Of<IAuditService>(), _context.Object));
        _sequences.Setup(s => s.NextAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await remove.Handle(new RemoveChannelMemberCommand(_workspaceId, _shared.Id, _guest), CancellationToken.None);

        _access.Verify(a => a.ChannelPerson(_workspaceId, _shared.Id, _guest), Times.Once);
        AsUser(_guest);
        await NotFound(() => GetChannel().Handle(new GetChannelQuery(_workspaceId, _shared.Id), CancellationToken.None));
        await NotFound(() => Authorize().Handle(new AuthorizeChannelRealtimeSubscriptionQuery(_shared.Id), CancellationToken.None));
        (await ListChannels().Handle(new ListChannelsQuery(_workspaceId), CancellationToken.None)).Data.Should().BeEmpty();
    }

    [Fact]
    public async Task A_deactivated_guest_gets_not_found_from_every_read_even_if_the_pipeline_did_not_stop_them()
    {
        // LEAK FIXED (defence in depth): the handlers took a suspended member's fact as the caller.
        _facts[_guest] = new WorkspaceMemberFact(_guest, "Guest", IsActive: false);

        await NotFound(() => ListChannels().Handle(new ListChannelsQuery(_workspaceId), CancellationToken.None));
        await NotFound(() => GetChannel().Handle(new GetChannelQuery(_workspaceId, _shared.Id), CancellationToken.None));
        await NotFound(() => ListMessages().Handle(new ListMessagesQuery(_workspaceId, _shared.Id, null, null, null, 50), CancellationToken.None));
        await NotFound(() => Authorize().Handle(new AuthorizeChannelRealtimeSubscriptionQuery(_shared.Id), CancellationToken.None));
    }

    // ---- What a guest cannot do (Governance baseline, the pipeline's first check) -----------------

    [Theory]
    [InlineData(PermissionAction.ChatChannelCreate)]
    [InlineData(PermissionAction.ChatChannelManage)]
    [InlineData(PermissionAction.ChatGroupManage)]
    [InlineData(PermissionAction.ChatMemberInvite)]
    [InlineData(PermissionAction.ChatMessageModerate)]
    [InlineData(PermissionAction.ChatMentionBroadcast)]
    [InlineData(PermissionAction.ChatMessagePin)]
    public void A_guest_holds_none_of_the_management_actions(PermissionAction action)
    {
        WorkspaceRoleBaseline.Decide("Guest", action).Should().Be(BaselineDecision.Deny);
    }

    [Fact]
    public void A_guest_is_never_shown_the_moderate_or_broadcast_capability()
    {
        var space = _shared;
        var membership = space.GetActiveMembership(_guest);

        var capabilities = Notrelix.Application.Features.Chat.Permissions.Services.ChannelPermissionChecks.Capabilities(space, "Guest", membership);

        capabilities.CanModerate.Should().BeFalse();
        capabilities.CanBroadcast.Should().BeFalse();
        capabilities.CanInvite.Should().BeFalse();
        capabilities.CanPin.Should().BeFalse();
        capabilities.CanManagePermissions.Should().BeFalse();
        capabilities.CanPost.Should().BeTrue();
    }
}
