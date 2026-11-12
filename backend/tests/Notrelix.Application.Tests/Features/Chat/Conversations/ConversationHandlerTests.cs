using Notrelix.Application.Common.Realtime;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Blocks.Commands.SetPersonBlock;
using Notrelix.Application.Features.Chat.Channels.Queries.GetChannel;
using Notrelix.Application.Features.Chat.Channels.Queries.ListChannels;
using Notrelix.Application.Features.Chat.Conversations.Commands.AddConversationParticipants;
using Notrelix.Application.Features.Chat.Conversations.Commands.LeaveConversation;
using Notrelix.Application.Features.Chat.Conversations.Commands.SetConversationVisibility;
using Notrelix.Application.Features.Chat.Conversations.Commands.StartConversation;
using Notrelix.Application.Features.Chat.Conversations.Queries.ListConversations;
using Notrelix.Application.Features.Chat.Members.Commands.JoinChannel;
using Notrelix.Application.Features.Chat.Messages.Commands.SendMessage;
using Notrelix.Application.Features.Chat.Messages.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Application.Tests.Features.Collaboration;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using BusinessRuleException = Notrelix.Domain.Common.Exceptions.BusinessRuleException;
using Space = Notrelix.Domain.Chat.Spaces.Space;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.Chat.Conversations;

/// <summary>
/// Conversations at the handler level (UC-CHT-E01 to E07, CHT-003, CHT-004, CHT-014): the idempotent pair, the race on one pair
/// (the lookup runs after the pair lock), group limits and skips, leave and close, hide and restore, block, and the guards that keep
/// channel commands off conversations.
/// </summary>
public class ConversationHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _alice = Guid.NewGuid();
    private readonly Guid _bob = Guid.NewGuid();
    private readonly Guid _carol = Guid.NewGuid();
    private readonly Guid _guest = Guid.NewGuid();
    private readonly Guid _outsider = Guid.NewGuid();
    private readonly Guid _deactivated = Guid.NewGuid();

    private readonly Mock<ICurrentRequestContext> _context = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IWorkspaceMemberFacts> _members = new();
    private readonly Mock<ISpaceSequenceAllocator> _sequences = new();
    private readonly Mock<IAccessChangeSignal> _access = new();
    private readonly Mock<IChatDbContext> _db = new();
    private readonly List<Space> _spaces = [];
    private readonly List<Membership> _memberships = [];
    private readonly List<Message> _messages = [];
    private readonly List<DirectPair> _pairs = [];
    private readonly List<PersonBlock> _blocks = [];
    private readonly List<ChannelGroup> _groups = [];
    private readonly List<Reaction> _reactions = [];
    private readonly RecordingPairLock _pairLock = new();
    private long _lastSequence;

    public ConversationHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(() => Now);
        _context.Setup(c => c.RequireAccountId()).Returns(_accountId);
        _context.Setup(c => c.IsAuthenticated).Returns(true);
        _sequences.Setup(s => s.LockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _sequences.Setup(s => s.NextAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => ++_lastSequence);

        _db.Setup(d => d.ChatSpaces).Returns(TestDbSet.Create(_spaces).Object);
        _db.Setup(d => d.ChatMemberships).Returns(TestDbSet.Create(_memberships).Object);
        _db.Setup(d => d.ChatPermissionOverrides).Returns(() => TestDbSet.Create(_spaces.SelectMany(s => s.PermissionOverrides).ToList()).Object);
        _db.Setup(d => d.ChatMessages).Returns(TestDbSet.Create(_messages).Object);
        _db.Setup(d => d.ChatMessageMentions).Returns(TestDbSet.Create(new List<MessageMention>()).Object);
        _db.Setup(d => d.ChatDirectPairs).Returns(TestDbSet.Create(_pairs).Object);
        _db.Setup(d => d.ChatBlocks).Returns(TestDbSet.Create(_blocks).Object);
        _db.Setup(d => d.ChatChannelGroups).Returns(TestDbSet.Create(_groups).Object);
        _db.Setup(d => d.ChatReactions).Returns(TestDbSet.Create(_reactions).Object);

        AsCaller(_alice, "Member");
        Fact(_bob, "Member", true);
        Fact(_carol, "Member", true);
        Fact(_guest, "Guest", true);
        Fact(_deactivated, "Member", false);
        // _outsider has no fact: not in the Workspace.
        _members.Setup(m => m.FindAsync(_workspaceId, _outsider, It.IsAny<CancellationToken>())).ReturnsAsync((WorkspaceMemberFact?)null);
    }

    private void Fact(Guid userId, string role, bool active) =>
        _members.Setup(m => m.FindAsync(_workspaceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(userId, role, active));

    private void AsCaller(Guid userId, string role)
    {
        _context.Setup(c => c.UserId).Returns(userId);
        Fact(userId, role, true);
    }

    private Mock<IChatDbContext> Db() => _db;
    private ChatSystemMessages SystemMessages() => new(_db.Object, _sequences.Object, _clock.Object);

    private StartConversationCommandHandler StartHandler() => new(_db.Object, _context.Object, _members.Object, _pairLock, _clock.Object);
    private AddConversationParticipantsCommandHandler AddHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object, SystemMessages());
    private LeaveConversationCommandHandler LeaveHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object, SystemMessages(), _access.Object);
    private SetConversationVisibilityCommandHandler VisibilityHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object);
    private ListConversationsQueryHandler ListHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object);
    private SetPersonBlockCommandHandler BlockHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object);

    private Space Direct(Guid first, Guid second)
    {
        var space = Space.CreateDirect(_accountId, _workspaceId, first, second, Now);
        _spaces.Add(space);
        _memberships.AddRange(space.Memberships);
        _pairs.Add(DirectPair.Create(_accountId, _workspaceId, first, second, space.Id));
        return space;
    }

    private Space Group(params Guid[] people)
    {
        var space = Space.CreateGroup(_accountId, _workspaceId, "Team", people.Skip(1), people[0], Now);
        _spaces.Add(space);
        _memberships.AddRange(space.Memberships);
        return space;
    }

    private Message Post(Space space, Guid author, string content, DateTimeOffset at)
    {
        var message = Message.CreateUserMessage(_accountId, _workspaceId, space.Id, space.LastSequence + 1, author, content, Guid.NewGuid().ToString(), null, at);
        _messages.Add(message);
        typeof(Space).GetProperty(nameof(Space.LastSequence))!.SetValue(space, message.Sequence);
        return message;
    }

    private static async Task<T> Expect<T>(Func<Task<T>> act) where T : notnull => await act();

    [Fact]
    public async Task StartDirect_CreatesOneConversationForThePair()
    {
        var result = await StartHandler().Handle(new StartConversationCommand(_workspaceId, "direct", _bob, null, null), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Created.Should().BeTrue();
        result.Data!.Conversation.Kind.Should().Be("direct");
        result.Data!.Conversation.Name.Should().BeNull();
        result.Data!.Conversation.Participants.Select(p => p.UserId).Should().BeEquivalentTo([_alice, _bob]);
        _spaces.Should().ContainSingle();
        _pairs.Should().ContainSingle(p => p.SpaceId == _spaces[0].Id);
    }

    [Fact]
    public async Task StartDirect_IsIdempotentForThePair_FromEitherSide()
    {
        var first = await StartHandler().Handle(new StartConversationCommand(_workspaceId, "direct", _bob, null, null), CancellationToken.None);
        var again = await StartHandler().Handle(new StartConversationCommand(_workspaceId, "direct", _bob, null, null), CancellationToken.None);

        AsCaller(_bob, "Member");
        var fromBob = await StartHandler().Handle(new StartConversationCommand(_workspaceId, "direct", _alice, null, null), CancellationToken.None);

        again.Data!.Created.Should().BeFalse("the pair already has its conversation");
        again.Data!.Conversation.Id.Should().Be(first.Data!.Conversation.Id);
        fromBob.Data!.Created.Should().BeFalse();
        fromBob.Data!.Conversation.Id.Should().Be(first.Data!.Conversation.Id);
        _spaces.Should().ContainSingle("one direct conversation per unordered pair");
    }

    [Fact]
    public async Task StartDirect_TakesThePairLockForTheOrderedPair()
    {
        await StartHandler().Handle(new StartConversationCommand(_workspaceId, "direct", _bob, null, null), CancellationToken.None);

        var (low, high) = DirectPair.Order(_alice, _bob);
        _pairLock.Calls.Should().ContainSingle().Which.Should().Be((_workspaceId, low, high));
    }

    [Fact]
    public async Task StartDirect_ReadsThePairAfterTheLock_SoARacingStartReturnsTheConversationItStored()
    {
        // The other start of the same pair commits while this one waits for the lock. The lookup runs after the lock, so this start
        // sees that conversation and stores nothing of its own.
        var racing = Space.CreateDirect(_accountId, _workspaceId, _alice, _bob, Now);
        _pairLock.OnLock = () =>
        {
            _spaces.Add(racing);
            _pairs.Add(DirectPair.Create(_accountId, _workspaceId, _alice, _bob, racing.Id));
            return Task.CompletedTask;
        };

        var result = await StartHandler().Handle(new StartConversationCommand(_workspaceId, "direct", _bob, null, null), CancellationToken.None);

        result.Data!.Created.Should().BeFalse();
        result.Data!.Conversation.Id.Should().Be(racing.Id);
        _spaces.Should().ContainSingle("the racing start is the only conversation of the pair");
        _pairs.Should().ContainSingle();
    }

    [Fact]
    public async Task StartDirect_WithYourself_IsA400SelfTarget()
    {
        var act = () => StartHandler().Handle(new StartConversationCommand(_workspaceId, "direct", _alice, null, null), CancellationToken.None);

        var error = await act.Should().ThrowAsync<BusinessRuleException>();
        error.Which.RuleCode.Should().Be(ChatRuleCodes.SelfTarget);
        _spaces.Should().BeEmpty();
    }

    [Fact]
    public async Task StartDirect_WithAPersonOutsideTheWorkspace_IsNotFound()
    {
        var act = () => StartHandler().Handle(new StartConversationCommand(_workspaceId, "direct", _outsider, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _spaces.Should().BeEmpty();
    }

    [Fact]
    public async Task StartDirect_WithADeactivatedPerson_IsNotFound()
    {
        var act = () => StartHandler().Handle(new StartConversationCommand(_workspaceId, "direct", _deactivated, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task StartDirect_BlockedByTheOtherPerson_IsNotFoundForTheBlockedSide()
    {
        _blocks.Add(PersonBlock.Create(_accountId, _workspaceId, _bob, _alice, Now));

        var act = () => StartHandler().Handle(new StartConversationCommand(_workspaceId, "direct", _bob, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>("the blocked person cannot start a conversation with the blocker");
    }

    [Fact]
    public async Task StartDirect_BlockedByTheCaller_IsNotFoundToo()
    {
        _blocks.Add(PersonBlock.Create(_accountId, _workspaceId, _alice, _bob, Now));

        var act = () => StartHandler().Handle(new StartConversationCommand(_workspaceId, "direct", _bob, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _pairLock.Calls.Should().BeEmpty("a refused start never takes the pair lock");
    }

    [Fact]
    public async Task StartDirect_ByAGuestWithoutASharedSpace_IsNotFound()
    {
        AsCaller(_guest, "Guest");

        var act = () => StartHandler().Handle(new StartConversationCommand(_workspaceId, "direct", _bob, null, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task StartDirect_ByAGuestWhoSharesASpaceWithThePerson_Succeeds()
    {
        var shared = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPrivate, "shared", null, null, 1, _alice, Now);
        shared.AddMember(_guest, MembershipRole.Member, Now);
        shared.AddMember(_bob, MembershipRole.Member, Now);
        _spaces.Add(shared);
        _memberships.AddRange(shared.Memberships);
        AsCaller(_guest, "Guest");

        var result = await StartHandler().Handle(new StartConversationCommand(_workspaceId, "direct", _bob, null, null), CancellationToken.None);

        result.Data!.Created.Should().BeTrue();
    }

    [Fact]
    public async Task StartDirect_WithAHiddenConversation_BringsItBackToTheCaller()
    {
        var existing = Direct(_alice, _bob);
        existing.GetActiveMembership(_alice)!.Hide(Now);

        var result = await StartHandler().Handle(new StartConversationCommand(_workspaceId, "direct", _bob, null, null), CancellationToken.None);

        result.Data!.Created.Should().BeFalse();
        existing.GetActiveMembership(_alice)!.IsHidden.Should().BeFalse();
    }

    [Fact]
    public async Task StartGroup_StoresTheCallerAndTheReachablePeopleAndReportsTheRest()
    {
        var result = await StartHandler().Handle(
            new StartConversationCommand(_workspaceId, "group", null, [_bob, _carol, _outsider], "  Launch  "), CancellationToken.None);

        result.Data!.Created.Should().BeTrue();
        result.Data!.Conversation.Name.Should().Be("Launch");
        result.Data!.Conversation.Participants.Select(p => p.UserId).Should().BeEquivalentTo([_alice, _bob, _carol]);
        result.Data!.Skipped.Should().ContainSingle().Which.Should().Be(_outsider);
    }

    [Fact]
    public async Task StartGroup_WithoutAName_HasNoName()
    {
        var result = await StartHandler().Handle(
            new StartConversationCommand(_workspaceId, "group", null, [_bob, _carol], null), CancellationToken.None);

        result.Data!.Conversation.Name.Should().BeNull();
    }

    [Fact]
    public async Task StartGroup_OverTheCap_IsLimitReached422()
    {
        var others = Enumerable.Range(0, ChatLimits.GroupCap).Select(_ => Guid.NewGuid()).ToList();

        var act = () => StartHandler().Handle(new StartConversationCommand(_workspaceId, "group", null, others, null), CancellationToken.None);

        var error = await act.Should().ThrowAsync<BusinessRuleException>();
        error.Which.RuleCode.Should().Be(ChatRuleCodes.LimitReached);
        _spaces.Should().BeEmpty();
    }

    [Fact]
    public async Task StartGroup_WhenTooFewCanBeAdded_IsRefused()
    {
        var act = () => StartHandler().Handle(
            new StartConversationCommand(_workspaceId, "group", null, [_bob, _outsider], null), CancellationToken.None);

        var error = await act.Should().ThrowAsync<BusinessRuleException>();
        error.Which.RuleCode.Should().Be(ChatRuleCodes.GroupTooSmall);
    }

    [Fact]
    public async Task AddParticipants_AddsPeopleAndAnnouncesEachOne()
    {
        var group = Group(_alice, _bob, _carol);
        var dave = Guid.NewGuid();
        Fact(dave, "Member", true);

        var result = await AddHandler().Handle(
            new AddConversationParticipantsCommand(_workspaceId, group.Id, [dave, _outsider]), CancellationToken.None);

        result.Data!.Added.Should().ContainSingle().Which.Should().Be(dave);
        result.Data!.Skipped.Should().ContainSingle().Which.Should().Be(_outsider);
        group.IsActiveMember(dave).Should().BeTrue();
        _messages.Should().ContainSingle(m => m.IsSystem && m.Content == ChatSystemMessages.MemberAdded);
    }

    [Fact]
    public async Task AddParticipants_StopsAtTheCap()
    {
        var people = Enumerable.Range(0, ChatLimits.GroupCap - 1).Select(_ => Guid.NewGuid()).ToList();
        var group = Space.CreateGroup(_accountId, _workspaceId, "Full", people, _alice, Now);
        _spaces.Add(group);
        var extra = Guid.NewGuid();
        Fact(extra, "Member", true);

        var act = () => AddHandler().Handle(new AddConversationParticipantsCommand(_workspaceId, group.Id, [extra]), CancellationToken.None);

        var error = await act.Should().ThrowAsync<BusinessRuleException>();
        error.Which.RuleCode.Should().Be(ChatRuleCodes.LimitReached);
    }

    [Fact]
    public async Task AddParticipants_ToADirectConversation_IsKindNotSupported()
    {
        var direct = Direct(_alice, _bob);

        var act = () => AddHandler().Handle(new AddConversationParticipantsCommand(_workspaceId, direct.Id, [_carol]), CancellationToken.None);

        var error = await act.Should().ThrowAsync<BusinessRuleException>();
        error.Which.RuleCode.Should().Be(ChatRuleCodes.KindNotSupported);
    }

    [Fact]
    public async Task AddParticipants_ByANonParticipant_IsNotFound()
    {
        var group = Group(_bob, _carol, Guid.NewGuid());

        var act = () => AddHandler().Handle(new AddConversationParticipantsCommand(_workspaceId, group.Id, [_guest]), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Leave_EndsTheMembershipAnnouncesItAndSignalsTheAccessChange()
    {
        var group = Group(_alice, _bob, _carol);

        await LeaveHandler().Handle(new LeaveConversationCommand(_workspaceId, group.Id), CancellationToken.None);

        group.IsActiveMember(_alice).Should().BeFalse();
        group.IsDeleted.Should().BeFalse("the others remain");
        _messages.Should().ContainSingle(m => m.IsSystem && m.Content == ChatSystemMessages.MemberLeft);
        _access.Verify(a => a.ChannelPerson(_workspaceId, group.Id, _alice), Times.Once);
    }

    [Fact]
    public async Task Leave_TwiceChangesNothingTheSecondTime()
    {
        var group = Group(_alice, _bob, _carol);
        await LeaveHandler().Handle(new LeaveConversationCommand(_workspaceId, group.Id), CancellationToken.None);

        AsCaller(_alice, "Member");
        var again = () => LeaveHandler().Handle(new LeaveConversationCommand(_workspaceId, group.Id), CancellationToken.None);

        await again.Should().ThrowAsync<NotFoundException>("a person who already left is no longer a participant");
    }

    [Fact]
    public async Task Leave_ADirectConversation_IsKindNotSupported()
    {
        var direct = Direct(_alice, _bob);

        var act = () => LeaveHandler().Handle(new LeaveConversationCommand(_workspaceId, direct.Id), CancellationToken.None);

        var error = await act.Should().ThrowAsync<BusinessRuleException>();
        error.Which.RuleCode.Should().Be(ChatRuleCodes.KindNotSupported);
    }

    [Fact]
    public async Task Leave_TheLastParticipant_ClosesTheConversation()
    {
        var group = Group(_alice, _bob, _carol);
        await LeaveHandler().Handle(new LeaveConversationCommand(_workspaceId, group.Id), CancellationToken.None);
        AsCaller(_bob, "Member");
        await LeaveHandler().Handle(new LeaveConversationCommand(_workspaceId, group.Id), CancellationToken.None);

        AsCaller(_carol, "Member");
        await LeaveHandler().Handle(new LeaveConversationCommand(_workspaceId, group.Id), CancellationToken.None);

        group.IsDeleted.Should().BeTrue("with no participant left the conversation closes");
    }

    [Fact]
    public async Task Leave_ByANonParticipant_IsNotFound()
    {
        var group = Group(_bob, _carol, Guid.NewGuid());

        var act = () => LeaveHandler().Handle(new LeaveConversationCommand(_workspaceId, group.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Hide_RemovesTheConversationFromTheCallersListOnly()
    {
        var direct = Direct(_alice, _bob);

        var hidden = await VisibilityHandler().Handle(new SetConversationVisibilityCommand(_workspaceId, direct.Id, true), CancellationToken.None);

        hidden.Data!.Hidden.Should().BeTrue();
        var aliceList = await ListHandler().Handle(new ListConversationsQuery(_workspaceId), CancellationToken.None);
        aliceList.Data!.Should().BeEmpty();

        AsCaller(_bob, "Member");
        var bobList = await ListHandler().Handle(new ListConversationsQuery(_workspaceId), CancellationToken.None);
        bobList.Data!.Should().ContainSingle(c => c.Id == direct.Id, "bob still sees it");
    }

    [Fact]
    public async Task Hide_IsIdempotentAndRestoreShowsItAgain()
    {
        var direct = Direct(_alice, _bob);
        await VisibilityHandler().Handle(new SetConversationVisibilityCommand(_workspaceId, direct.Id, true), CancellationToken.None);

        var again = await VisibilityHandler().Handle(new SetConversationVisibilityCommand(_workspaceId, direct.Id, true), CancellationToken.None);
        again.Data!.Hidden.Should().BeTrue();

        var restored = await VisibilityHandler().Handle(new SetConversationVisibilityCommand(_workspaceId, direct.Id, false), CancellationToken.None);
        restored.Data!.Hidden.Should().BeFalse();
        var list = await ListHandler().Handle(new ListConversationsQuery(_workspaceId), CancellationToken.None);
        list.Data!.Should().ContainSingle();
    }

    [Fact]
    public async Task Hide_ByANonParticipant_IsNotFound()
    {
        var direct = Direct(_bob, _carol);

        var act = () => VisibilityHandler().Handle(new SetConversationVisibilityCommand(_workspaceId, direct.Id, true), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ANewMessage_ShowsAHiddenConversationAgainForEveryone()
    {
        var direct = Direct(_alice, _bob);
        await VisibilityHandler().Handle(new SetConversationVisibilityCommand(_workspaceId, direct.Id, true), CancellationToken.None);
        AsCaller(_bob, "Member");
        await VisibilityHandler().Handle(new SetConversationVisibilityCommand(_workspaceId, direct.Id, true), CancellationToken.None);

        AsCaller(_alice, "Member");
        var send = new SendMessageCommandHandler(_db.Object, _context.Object, _members.Object, _sequences.Object, _clock.Object, new Mock<Notrelix.Application.Features.Chat.Mentions.Services.IBroadcastAuthorizer>().Object, new InMemoryChatMessageAttachments());
        await send.Handle(new SendMessageCommand(_workspaceId, direct.Id, "are you there?", "client-1", null), CancellationToken.None);

        direct.GetActiveMembership(_bob)!.IsHidden.Should().BeFalse("the message restores the conversation for bob");
        direct.GetActiveMembership(_alice)!.IsHidden.Should().BeFalse();
    }

    [Fact]
    public async Task List_ShowsOnlyMyConversationsNewestActivityFirst_WithPreviewsAndUnread()
    {
        var older = Direct(_alice, _bob);
        Post(older, _bob, "old", Now.AddHours(-3));
        var newer = Group(_alice, _bob, _carol);
        Post(newer, _carol, "  the launch is on friday  ", Now.AddHours(-1));
        var notMine = Direct(_bob, _carol);
        Post(notMine, _bob, "private", Now);

        var result = await ListHandler().Handle(new ListConversationsQuery(_workspaceId), CancellationToken.None);

        result.Data!.Select(c => c.Id).Should().Equal(newer.Id, older.Id);
        var first = result.Data![0];
        first.LastMessage!.Text.Should().Be("the launch is on friday");
        first.LastMessage!.AuthorId.Should().Be(_carol);
        first.Participants.Select(p => p.UserId).Should().BeEquivalentTo([_alice, _bob, _carol]);
        result.Data![1].UnreadCount.Should().Be(1, "bob's message is unread for alice");
    }

    [Fact]
    public async Task List_LeavesOutChannelsAndRespectsTheLimit()
    {
        var channel = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "general", null, null, 1, _alice, Now);
        _spaces.Add(channel);
        _memberships.AddRange(channel.Memberships);
        Direct(_alice, _bob);
        Direct(_alice, _carol);

        var all = await ListHandler().Handle(new ListConversationsQuery(_workspaceId), CancellationToken.None);
        var one = await ListHandler().Handle(new ListConversationsQuery(_workspaceId, 1), CancellationToken.None);

        all.Data!.Should().HaveCount(2);
        all.Data!.Should().OnlyContain(c => c.Kind == "direct");
        one.Data!.Should().HaveCount(1);
    }

    [Fact]
    public async Task Block_IsIdempotentAndUnblockRemovesIt()
    {
        var first = await BlockHandler().Handle(new SetPersonBlockCommand(_workspaceId, _bob, true), CancellationToken.None);
        var again = await BlockHandler().Handle(new SetPersonBlockCommand(_workspaceId, _bob, true), CancellationToken.None);
        _blocks.Should().ContainSingle(b => b.BlockerId == _alice && b.BlockedId == _bob);
        first.Data!.Blocked.Should().BeTrue();
        again.Data!.Blocked.Should().BeTrue();

        var removed = await BlockHandler().Handle(new SetPersonBlockCommand(_workspaceId, _bob, false), CancellationToken.None);

        removed.Data!.Blocked.Should().BeFalse();
        _blocks.Should().BeEmpty();
    }

    [Fact]
    public async Task Block_YourselfIsA400SelfTarget()
    {
        var act = () => BlockHandler().Handle(new SetPersonBlockCommand(_workspaceId, _alice, true), CancellationToken.None);

        var error = await act.Should().ThrowAsync<BusinessRuleException>();
        error.Which.RuleCode.Should().Be(ChatRuleCodes.SelfTarget);
    }

    [Fact]
    public async Task Block_OfAPersonOutsideTheWorkspace_IsNotFound()
    {
        var act = () => BlockHandler().Handle(new SetPersonBlockCommand(_workspaceId, _outsider, true), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ChannelCommands_DoNotReachAConversation()
    {
        var direct = Direct(_alice, _bob);
        var join = new JoinChannelCommandHandler(_db.Object, _context.Object, _members.Object, _clock.Object, SystemMessages());
        var get = new GetChannelQueryHandler(_db.Object, _context.Object, _members.Object);

        AsCaller(_carol, "Member");
        var joinAct = () => join.Handle(new JoinChannelCommand(_workspaceId, direct.Id), CancellationToken.None);
        var getAct = () => get.Handle(new GetChannelQuery(_workspaceId, direct.Id), CancellationToken.None);

        await joinAct.Should().ThrowAsync<NotFoundException>("a direct conversation is not a public channel");
        await getAct.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ListChannels_DoesNotListConversations()
    {
        Direct(_alice, _bob);
        var channels = new ListChannelsQueryHandler(_db.Object, _context.Object, _members.Object);

        var result = await channels.Handle(new ListChannelsQuery(_workspaceId), CancellationToken.None);

        result.Data!.Should().BeEmpty();
    }
}

/// <summary>A pair lock that records its calls and can run a step at the moment the lock is taken (the racing start).</summary>
internal sealed class RecordingPairLock : IDirectConversationLock
{
    public List<(Guid Workspace, Guid Low, Guid High)> Calls { get; } = [];
    public Func<Task>? OnLock { get; set; }

    public async Task LockPairAsync(Guid workspaceId, Guid userLow, Guid userHigh, CancellationToken cancellationToken)
    {
        Calls.Add((workspaceId, userLow, userHigh));
        if (OnLock is not null) await OnLock();
    }
}
