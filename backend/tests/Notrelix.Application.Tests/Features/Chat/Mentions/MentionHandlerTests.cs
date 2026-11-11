using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Testing.Application.Fakes;
using Notrelix.Application.Common.Models;
using Notrelix.Application.Features.Chat.Mentions.DTOs;
using Notrelix.Application.Features.Chat.Messages.DTOs;
using Notrelix.Application.Features.Chat.Mentions.Queries.ListMentionables;
using Notrelix.Application.Features.Chat.Mentions.Services;
using Notrelix.Application.Features.Chat.Messages.Commands.DeleteMessage;
using Notrelix.Application.Features.Chat.Messages.Commands.EditMessage;
using Notrelix.Application.Features.Chat.Messages.Commands.SendMessage;
using Notrelix.Application.Features.Chat.Messages.Queries.ListMessages;
using Notrelix.Application.Features.Chat.ReadState.Queries.GetUnreadSummary;
using Notrelix.Application.Features.Chat.Threads.Commands.ReplyInThread;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Application.Tests.Features.Collaboration;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using AppConflictException = Notrelix.Application.Common.Exceptions.ConflictException;
using AppNotFoundException = Notrelix.Application.Common.Exceptions.NotFoundException;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Tests.Features.Chat.Mentions;

/// <summary>
/// Mentions and broadcast (UC-CHT-G01 to G04) and the mention counters (UC-CHT-D04, CHT-010, COL-030) at handler level with fakes.
/// The server validates every mention against the space; the privacy cases prove that no one learns of a private channel or of a
/// person they could not see.
/// </summary>
public class MentionHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _author = Guid.NewGuid();
    private readonly Guid _bob = Guid.NewGuid();
    private readonly Guid _carol = Guid.NewGuid();
    private readonly Guid _guest = Guid.NewGuid();
    private readonly Guid _colleague = Guid.NewGuid();
    private readonly Guid _stranger = Guid.NewGuid();

    private readonly Mock<ICurrentRequestContext> _context = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IWorkspaceMemberFacts> _members = new();
    private readonly Mock<IBroadcastAuthorizer> _broadcast = new();
    private readonly List<Space> _spaces = [];
    private readonly List<Message> _messages = [];
    private readonly List<MessageMention> _mentions = [];
    private readonly Mock<IChatDbContext> _db = new();
    private readonly Mock<ISpaceSequenceAllocator> _sequences = new();
    private long _lastSequence;

    public MentionHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(() => Now);
        _context.Setup(c => c.RequireAccountId()).Returns(_accountId);
        _context.Setup(c => c.RequireWorkspaceId()).Returns(_workspaceId);
        _context.Setup(c => c.IsAuthenticated).Returns(true);
        _context.Setup(c => c.UserId).Returns(_author);

        _db.Setup(d => d.ChatSpaces).Returns(() => TestDbSet.Create(_spaces).Object);
        _db.Setup(d => d.ChatMemberships).Returns(() => TestDbSet.Create(_spaces.SelectMany(s => s.Memberships).ToList()).Object);
        _db.Setup(d => d.ChatChannelGroups).Returns(TestDbSet.Create(new List<ChannelGroup>()).Object);
        _db.Setup(d => d.ChatMessages).Returns(() => TestDbSet.Create(_messages).Object);
        _db.Setup(d => d.ChatReactions).Returns(() => TestDbSet.Create(new List<Reaction>()).Object);
        _db.Setup(d => d.ChatPins).Returns(TestDbSet.Create(new List<Pin>()).Object);
        _db.Setup(d => d.ChatThreads).Returns(TestDbSet.Create(new List<MessageThread>()).Object);
        _db.Setup(d => d.ChatThreadFollowers).Returns(TestDbSet.Create(new List<ThreadFollower>()).Object);
        _db.Setup(d => d.ChatMessageMentions).Returns(() => TestDbSet.Create(_mentions).Object);

        _sequences.Setup(s => s.LockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _sequences.Setup(s => s.NextAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => ++_lastSequence);
        _broadcast.Setup(b => b.CanBroadcastAsync(_workspaceId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Fact(_owner, "Owner");
        Fact(_author, "Member");
        Fact(_bob, "Member");
        Fact(_carol, "Member");
        Fact(_guest, "Guest");
        Fact(_colleague, "Member");
        // _stranger is no one of this Workspace: no fact.
        _members.Setup(m => m.ListActiveColleagueIdsAsync(_workspaceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([_owner, _author, _bob, _carol, _colleague]);
    }

    private void Fact(Guid userId, string role) =>
        _members.Setup(m => m.FindAsync(_workspaceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(userId, role, true));

    private void AsUser(Guid userId) => _context.Setup(c => c.UserId).Returns(userId);

    private Space Channel(SpaceKind kind = SpaceKind.ChannelPublic, string name = "general", params Guid[] extraMembers)
    {
        var space = Space.CreateChannel(_accountId, _workspaceId, kind, name, null, null, 1, _owner, Now);
        space.AddMember(_author, MembershipRole.Member, Now);
        space.AddMember(_bob, MembershipRole.Member, Now);
        foreach (var id in extraMembers)
            space.AddMember(id, MembershipRole.Member, Now);
        _spaces.Add(space);
        return space;
    }

    private SendMessageCommandHandler SendHandler() => new(_db.Object, _context.Object, _members.Object, _sequences.Object, _clock.Object, _broadcast.Object, new InMemoryChatMessageAttachments());
    private EditMessageCommandHandler EditHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object, _broadcast.Object, new InMemoryChatMessageAttachments());
    private ReplyInThreadCommandHandler ReplyHandler() => new(_db.Object, _context.Object, _members.Object, _sequences.Object, _clock.Object, _broadcast.Object, new InMemoryChatMessageAttachments());
    private DeleteMessageCommandHandler DeleteHandler() => new(_db.Object, _context.Object, _members.Object, _sequences.Object, _clock.Object, new InMemoryChatMessageAttachments());
    private ListMessagesQueryHandler ListHandler() => new(_db.Object, _context.Object, _members.Object, new InMemoryChatMessageAttachments());
    private ListMentionablesQueryHandler MentionablesHandler() => new(_db.Object, _context.Object, _members.Object, _broadcast.Object);

    private static MentionInput Person(int start, int length, Guid id) => new(MentionKindWire.UserKind, start, length, id, null);
    private static MentionInput Link(int start, int length, Guid spaceId) => new(MentionKindWire.ChannelLinkKind, start, length, null, spaceId);
    private static MentionInput Channel(int start) => new(MentionKindWire.BroadcastChannelKind, start, 8, null, null);
    private static MentionInput Here(int start) => new(MentionKindWire.BroadcastHereKind, start, 5, null, null);

    private Task<Result<SendMessageResultDto>> Send(
        Space space, string content, IReadOnlyList<MentionInput>? mentions, string clientId = "c-1", bool confirm = false) =>
        SendHandler().Handle(
            new SendMessageCommand(_workspaceId, space.Id, content, clientId, null, mentions, confirm), CancellationToken.None);

    private Task<Result<Notrelix.Application.Features.Chat.ReadState.DTOs.UnreadSummaryDto>> Summary() =>
        new GetUnreadSummaryQueryHandler(_db.Object, _context.Object, _members.Object, _clock.Object)
            .Handle(new GetUnreadSummaryQuery(_workspaceId), CancellationToken.None);

    private async Task<int> MentionsOf(Guid userId, Space space)
    {
        AsUser(userId);
        var summary = await Summary();
        return summary.Data!.Spaces.Single(s => s.SpaceId == space.Id).MentionCount;
    }

    // ── G01: mention a person ────────────────────────────────────────────────

    [Fact]
    public async Task Send_WithAMentionOfAMember_StoresItByStableIdAndAnswersWithTheValidatedList()
    {
        var space = Channel();

        var result = await Send(space, "hi @Bob", [Person(3, 4, _bob)]);

        result.Succeeded.Should().BeTrue();
        result.Data!.Message.Mentions.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new MentionDto("user", 3, 4, _bob, null));
        result.Data.Message.MentionNotices.Should().BeEmpty();
        _mentions.Should().ContainSingle(m => m.TargetId == _bob && m.Kind == MentionKind.User && m.MessageId == result.Data.Message.Id);
    }

    [Fact]
    public async Task Send_MovesTheSpansOntoTheTrimmedText()
    {
        var space = Channel();

        var result = await Send(space, "   hi @Bob  ", [Person(6, 4, _bob)]);

        result.Data!.Message.Content.Should().Be("hi @Bob");
        result.Data.Message.Mentions.Single().Start.Should().Be(3);
    }

    [Fact]
    public async Task Send_WithoutMentions_StoresNoRows()
    {
        var space = Channel();

        var result = await Send(space, "plain", null);

        result.Data!.Message.Mentions.Should().BeEmpty();
        _mentions.Should().BeEmpty();
    }

    [Theory]
    [InlineData("hi @Bob", 3, 20)]   // runs past the text
    [InlineData("hi @Bob", -1, 4)]   // before the text
    [InlineData("hi Bob", 3, 3)]     // does not start with @
    [InlineData("hi @Bob", 3, 0)]    // empty
    public async Task Send_WithAMentionThatDoesNotFitTheText_IsRefused(string content, int start, int length)
    {
        var space = Channel();

        var act = () => Send(space, content, [Person(start, length, _bob)]);

        var error = await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        error.Which.RuleCode.Should().Be(ChatRuleCodes.MentionInvalid);
        _messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Send_WithOverlappingMentions_IsRefused()
    {
        var space = Channel();

        var act = () => Send(space, "hi @Bob", [Person(3, 4, _bob), Person(4, 3, _carol)]);

        var error = await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        error.Which.RuleCode.Should().Be(ChatRuleCodes.MentionInvalid);
    }

    [Fact]
    public async Task Send_WithAnUnknownMentionKind_IsRefused()
    {
        var space = Channel();

        var act = () => Send(space, "hi @Bob", [new MentionInput("team", 3, 4, _bob, null)]);

        var error = await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        error.Which.RuleCode.Should().Be(ChatRuleCodes.MentionInvalid);
    }

    [Fact]
    public async Task Send_WithMoreMentionsThanTheLimit_IsRefused()
    {
        var space = Channel();
        var text = string.Join(' ', Enumerable.Repeat("@b", ChatLimits.MentionsPerMessageMax + 1));
        var mentions = Enumerable.Range(0, ChatLimits.MentionsPerMessageMax + 1).Select(i => Person(i * 3, 2, _bob)).ToList();

        var act = () => Send(space, text, mentions);

        var error = await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        error.Which.RuleCode.Should().Be(ChatRuleCodes.MentionLimitExceeded);
    }

    // ── G02: a mention that cannot be seen is a warning, not a leak ──────────

    [Fact]
    public async Task Send_MentioningAWorkspaceMemberWhoIsNotInAPrivateChannel_StoresItAndWarnsTheSender()
    {
        var space = Channel(SpaceKind.ChannelPrivate, "secret");

        var result = await Send(space, "ping @Colleague", [Person(5, 10, _colleague)]);

        result.Data!.Message.Mentions.Should().ContainSingle(m => m.UserId == _colleague);
        result.Data.Message.MentionNotices.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new MentionNoticeDto(MentionResolver.CannotView, 5, 10, _colleague));
    }

    [Fact]
    public async Task AMentionOfSomeoneWhoCannotSeeTheChannel_IsNeverCountedForThem()
    {
        var space = Channel(SpaceKind.ChannelPrivate, "secret");
        await Send(space, "ping @Colleague", [Person(5, 10, _colleague)]);

        // The colleague is in no space, so their unread summary lists no space at all and shows nothing of the channel.
        AsUser(_colleague);
        var summary = await Summary();

        summary.Data!.Spaces.Should().BeEmpty();
        summary.Data.TotalMentions.Should().Be(0);
    }

    [Fact]
    public async Task Send_MentioningSomeoneOutsideTheWorkspace_ShowsTextAndStoresNothing()
    {
        var space = Channel();

        var result = await Send(space, "hi @Nobody", [Person(3, 7, _stranger)]);

        result.Data!.Message.Mentions.Should().BeEmpty();
        result.Data.Message.MentionNotices.Should().ContainSingle()
            .Which.Code.Should().Be(MentionResolver.Unknown);
        result.Data.Message.MentionNotices.Single().UserId.Should().BeNull();
        _mentions.Should().BeEmpty();
    }

    [Fact]
    public async Task AGuestMentioningAPersonOutsideTheirSpaces_LearnsNothingAboutTheWorkspace()
    {
        var space = Channel(SpaceKind.ChannelPublic, "general", _guest);
        AsUser(_guest);

        var inWorkspace = await Send(space, "hi @Col", [Person(3, 4, _colleague)], "g-1");
        var outside = await Send(space, "hi @Str", [Person(3, 4, _stranger)], "g-2");

        // A colleague in the Workspace and a stranger look the same to a Guest: plain text, same notice, nothing stored.
        inWorkspace.Data!.Message.MentionNotices.Single().Code.Should().Be(MentionResolver.Unknown);
        outside.Data!.Message.MentionNotices.Single().Code.Should().Be(MentionResolver.Unknown);
        _mentions.Should().BeEmpty();
    }

    [Fact]
    public async Task Send_RetryWithTheSameClientId_ReturnsTheStoredMessageStoresNothingAndRepeatsTheWarning()
    {
        var space = Channel(SpaceKind.ChannelPrivate, "secret");
        var first = await Send(space, "ping @Colleague", [Person(5, 10, _colleague)], "same");

        var retry = await Send(space, "ping @Colleague", [Person(5, 10, _colleague)], "same");

        retry.Data!.Created.Should().BeFalse();
        retry.Data.Message.Id.Should().Be(first.Data!.Message.Id);
        retry.Data.Message.Mentions.Should().ContainSingle(m => m.UserId == _colleague);
        retry.Data.Message.MentionNotices.Should().ContainSingle(n => n.Code == MentionResolver.CannotView && n.UserId == _colleague);
        _messages.Should().ContainSingle();
        _mentions.Should().ContainSingle();
    }

    // ── G03: broadcast ───────────────────────────────────────────────────────

    [Fact]
    public async Task Send_WithABroadcastByACallerWithTheAction_IsStored()
    {
        var space = Channel();

        var result = await Send(space, "hey @channel", [Channel(4)]);

        result.Data!.Message.Mentions.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new MentionDto("broadcast-channel", 4, 8, null, null));
        result.Data.Message.MentionNotices.Should().BeEmpty();
    }

    [Fact]
    public async Task Send_WithAHereBroadcast_IsStoredAsHere()
    {
        var space = Channel();

        var result = await Send(space, "hey @here", [Here(4)]);

        result.Data!.Message.Mentions.Single().Kind.Should().Be("broadcast-here");
    }

    [Fact]
    public async Task Send_WithABroadcastByACallerWithoutTheAction_IsPlainTextWithANotice()
    {
        var space = Channel();
        _broadcast.Setup(b => b.CanBroadcastAsync(_workspaceId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await Send(space, "hey @channel", [Channel(4)]);

        result.Succeeded.Should().BeTrue();
        result.Data!.Message.Mentions.Should().BeEmpty();
        result.Data.Message.MentionNotices.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new MentionNoticeDto(MentionResolver.BroadcastNotAllowed, 4, 8, null));
        _mentions.Should().BeEmpty();
    }

    [Fact]
    public async Task AGuestCannotBroadcast_EvenWhenTheActionWouldAllowIt()
    {
        var space = Channel(SpaceKind.ChannelPublic, "general", _guest);
        AsUser(_guest);

        var result = await Send(space, "hey @channel", [Channel(4)]);

        result.Data!.Message.Mentions.Should().BeEmpty();
        result.Data.Message.MentionNotices.Single().Code.Should().Be(MentionResolver.BroadcastNotAllowed);
        _broadcast.Verify(b => b.CanBroadcastAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Send_WithABroadcastInAChannelWithNoOneElse_IsPlainTextWithANotice()
    {
        var space = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "solo", null, null, 1, _owner, Now);
        _spaces.Add(space);
        AsUser(_owner);

        var result = await Send(space, "hey @channel", [Channel(4)]);

        result.Data!.Message.Mentions.Should().BeEmpty();
        result.Data.Message.MentionNotices.Single().Code.Should().Be(MentionResolver.NoAudience);
    }

    [Fact]
    public async Task Send_WithABroadcastAboveTheMemberThreshold_NeedsTheConfirmation()
    {
        var many = Enumerable.Range(0, ChatLimits.BroadcastConfirmThreshold).Select(_ => Guid.NewGuid()).ToArray();
        var space = Channel(SpaceKind.ChannelPublic, "big", many);

        var unconfirmed = () => Send(space, "hey @channel", [Channel(4)]);
        var error = await unconfirmed.Should().ThrowAsync<AppConflictException>();
        error.Which.ErrorCode.Should().Be(ChatRuleCodes.BroadcastConfirmationRequired);
        _messages.Should().BeEmpty();

        var confirmed = await Send(space, "hey @channel", [Channel(4)], "c-2", confirm: true);
        confirmed.Data!.Message.Mentions.Should().ContainSingle();
    }

    [Fact]
    public async Task Send_WithABroadcastAtTheThreshold_NeedsNoConfirmation()
    {
        // Three members already (the creator, author, bob); the threshold counts active members and is "more than".
        var many = Enumerable.Range(0, ChatLimits.BroadcastConfirmThreshold - 3).Select(_ => Guid.NewGuid()).ToArray();
        var space = Channel(SpaceKind.ChannelPublic, "edge", many);
        space.Memberships.Count(m => m.IsActive).Should().Be(ChatLimits.BroadcastConfirmThreshold);

        var result = await Send(space, "hey @channel", [Channel(4)]);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ABroadcastCountsAsAMentionForEveryMemberExceptTheAuthor()
    {
        var space = Channel(SpaceKind.ChannelPublic, "general", _carol);
        await Send(space, "hey @channel", [Channel(4)]);

        (await MentionsOf(_bob, space)).Should().Be(1);
        (await MentionsOf(_carol, space)).Should().Be(1);
        (await MentionsOf(_author, space)).Should().Be(0);
    }

    // ── G04: link to another channel ─────────────────────────────────────────

    [Fact]
    public async Task Send_WithALinkToAPublicChannel_StoresItById()
    {
        var space = Channel();
        var target = Channel(SpaceKind.ChannelPublic, "design");

        var result = await Send(space, "see #design", [Link(4, 7, target.Id)]);

        result.Data!.Message.Mentions.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new MentionDto("channel-link", 4, 7, null, target.Id));
    }

    [Fact]
    public async Task Send_WithALinkToAPrivateChannelTheSenderIsNotIn_IsPlainTextAndLooksLikeAMissingChannel()
    {
        var space = Channel();
        var hidden = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPrivate, "board", null, null, 2, _owner, Now);
        hidden.AddMember(_carol, MembershipRole.Member, Now);
        _spaces.Add(hidden);

        var toPrivate = await Send(space, "see #board", [Link(4, 6, hidden.Id)], "c-1");
        var toMissing = await Send(space, "see #gone", [Link(4, 5, Guid.NewGuid())], "c-2");

        toPrivate.Data!.Message.Mentions.Should().BeEmpty();
        toPrivate.Data.Message.MentionNotices.Should().ContainSingle(n => n.Code == MentionResolver.LinkNotVisible);
        toMissing.Data!.Message.MentionNotices.Should().ContainSingle(n => n.Code == MentionResolver.LinkNotVisible);
        _mentions.Should().BeEmpty();
    }

    [Fact]
    public async Task AReaderWhoCannotSeeALinkedPrivateChannel_ReceivesNoLinkForIt()
    {
        var space = Channel(SpaceKind.ChannelPublic, "general", _carol);
        var priv = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPrivate, "board", null, null, 2, _owner, Now);
        priv.AddMember(_author, MembershipRole.Member, Now);
        _spaces.Add(priv);
        var sent = await Send(space, "see #board", [Link(4, 6, priv.Id)]);
        sent.Data!.Message.Mentions.Should().ContainSingle();

        AsUser(_author);
        var forAuthor = await ListHandler().Handle(new ListMessagesQuery(_workspaceId, space.Id, null, null, null, 50), CancellationToken.None);
        AsUser(_carol);
        var forCarol = await ListHandler().Handle(new ListMessagesQuery(_workspaceId, space.Id, null, null, null, 50), CancellationToken.None);

        forAuthor.Data!.Items.Single().Mentions.Should().ContainSingle(m => m.SpaceId == priv.Id);
        forCarol.Data!.Items.Single().Mentions.Should().BeEmpty();
    }

    // ── Edit, delete, thread reply ───────────────────────────────────────────

    [Fact]
    public async Task Edit_WithNewText_ReplacesTheMentions()
    {
        var space = Channel(SpaceKind.ChannelPublic, "general", _carol);
        var sent = await Send(space, "hi @Bob", [Person(3, 4, _bob)]);

        var edited = await EditHandler().Handle(
            new EditMessageCommand(_workspaceId, sent.Data!.Message.Id, "hi @Carol", sent.Data.Message.Version, [Person(3, 6, _carol)]),
            CancellationToken.None);

        edited.Data!.Mentions.Should().ContainSingle(m => m.UserId == _carol);
        _mentions.Should().ContainSingle(m => m.TargetId == _carol);
        _mentions.Should().NotContain(m => m.TargetId == _bob);
    }

    [Fact]
    public async Task Edit_WithTheSameText_ChangesNothing()
    {
        var space = Channel();
        var sent = await Send(space, "hi @Bob", [Person(3, 4, _bob)]);

        var edited = await EditHandler().Handle(
            new EditMessageCommand(_workspaceId, sent.Data!.Message.Id, "hi @Bob", sent.Data.Message.Version),
            CancellationToken.None);

        edited.Data!.Mentions.Should().ContainSingle(m => m.UserId == _bob);
        _mentions.Should().ContainSingle();
    }

    [Fact]
    public async Task Delete_RemovesTheMentionsOfTheMessage()
    {
        var space = Channel();
        var sent = await Send(space, "hi @Bob", [Person(3, 4, _bob)]);

        var deleted = await DeleteHandler().Handle(
            new DeleteMessageCommand(_workspaceId, sent.Data!.Message.Id, sent.Data.Message.Version), CancellationToken.None);

        deleted.Succeeded.Should().BeTrue();
        _mentions.Should().BeEmpty();
    }

    [Fact]
    public async Task ReplyInThread_StoresItsMentionsLikeASend()
    {
        var space = Channel();
        var root = await Send(space, "root", null, "root-1");

        var reply = await ReplyHandler().Handle(
            new ReplyInThreadCommand(_workspaceId, root.Data!.Message.Id, "cc @Bob", "reply-1", false, [Person(3, 4, _bob)]),
            CancellationToken.None);

        reply.Data!.Message.Mentions.Should().ContainSingle(m => m.UserId == _bob);
        (await MentionsOf(_bob, space)).Should().Be(1);
    }

    // ── D04: mention counters ────────────────────────────────────────────────

    [Fact]
    public async Task AMention_CountsForTheMentionedPersonOnly_AndBadgesTheTotal()
    {
        var space = Channel(SpaceKind.ChannelPublic, "general", _carol);
        await Send(space, "hi @Bob", [Person(3, 4, _bob)]);

        (await MentionsOf(_bob, space)).Should().Be(1);
        (await MentionsOf(_carol, space)).Should().Be(0);

        AsUser(_bob);
        (await Summary()).Data!.TotalMentions.Should().Be(1);
    }

    [Fact]
    public async Task TwoMentionsOfOnePersonInOneMessage_CountOnce()
    {
        var space = Channel();
        await Send(space, "@Bob and @Bob", [Person(0, 4, _bob), Person(9, 4, _bob)]);

        (await MentionsOf(_bob, space)).Should().Be(1);
    }

    [Fact]
    public async Task ReadingPastAMention_ClearsIt()
    {
        var space = Channel();
        await Send(space, "hi @Bob", [Person(3, 4, _bob)]);
        space.GetActiveMembership(_bob)!.AdvanceReadSequence(_lastSequence);

        (await MentionsOf(_bob, space)).Should().Be(0);
    }

    [Fact]
    public async Task AMentionInAMutedSpace_CountsButDoesNotBadge()
    {
        var space = Channel();
        space.GetActiveMembership(_bob)!.SetNotificationLevel(NotificationLevel.None, null, Now);
        await Send(space, "hi @Bob", [Person(3, 4, _bob)]);

        AsUser(_bob);
        var summary = (await Summary()).Data!;

        summary.Spaces.Single().MentionCount.Should().Be(1);
        summary.Spaces.Single().Muted.Should().BeTrue();
        summary.TotalMentions.Should().Be(0);
    }

    [Fact]
    public async Task ADeletedMessage_NoLongerCountsAsAMention()
    {
        var space = Channel();
        var sent = await Send(space, "hi @Bob", [Person(3, 4, _bob)]);
        await DeleteHandler().Handle(
            new DeleteMessageCommand(_workspaceId, sent.Data!.Message.Id, sent.Data.Message.Version), CancellationToken.None);

        (await MentionsOf(_bob, space)).Should().Be(0);
    }

    [Fact]
    public async Task APersonWhoJoinsLater_DoesNotCountMentionsFromBeforeTheyJoined()
    {
        var space = Channel(SpaceKind.ChannelPrivate, "secret");
        await Send(space, "ping @Colleague", [Person(5, 10, _colleague)]);

        // The colleague joins after the message was sent: the mention was a warning then, and grants and counts nothing now.
        typeof(Membership).GetProperty(nameof(Membership.JoinedAt))!.SetValue(
            space.Memberships.Single(m => m.UserId == _bob), Now);
        space.AddMember(_colleague, MembershipRole.Member, Now.AddMinutes(5));

        (await MentionsOf(_colleague, space)).Should().Be(0);
    }

    // ── The mention picker ───────────────────────────────────────────────────

    [Fact]
    public async Task Mentionables_ListsTheMembersFirstThenTheRestOfTheWorkspaceMarkedNotInSpace()
    {
        var space = Channel(SpaceKind.ChannelPrivate, "secret");

        var result = await MentionablesHandler().Handle(new ListMentionablesQuery(_workspaceId, space.Id), CancellationToken.None);

        var people = result.Data!.People;
        people.Where(p => p.InSpace).Select(p => p.UserId).Should().BeEquivalentTo([_owner, _author, _bob]);
        people.Where(p => !p.InSpace).Select(p => p.UserId).Should().BeEquivalentTo([_carol, _colleague]);
        people.Take(3).Should().OnlyContain(p => p.InSpace);
        result.Data.CanBroadcast.Should().BeTrue();
        result.Data.BroadcastConfirmAbove.Should().Be(ChatLimits.BroadcastConfirmThreshold);
        result.Data.MemberCount.Should().Be(3);
    }

    [Fact]
    public async Task Mentionables_ForAGuest_ListsThePeopleOfTheSpaceOnlyAndNoBroadcast()
    {
        var space = Channel(SpaceKind.ChannelPublic, "general", _guest);
        AsUser(_guest);

        var result = await MentionablesHandler().Handle(new ListMentionablesQuery(_workspaceId, space.Id), CancellationToken.None);

        result.Data!.People.Should().OnlyContain(p => p.InSpace);
        result.Data.People.Select(p => p.UserId).Should().BeEquivalentTo([_owner, _author, _bob, _guest]);
        result.Data.CanBroadcast.Should().BeFalse();
    }

    [Fact]
    public async Task Mentionables_WithoutTheBroadcastAction_SaysSo()
    {
        var space = Channel();
        _broadcast.Setup(b => b.CanBroadcastAsync(_workspaceId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await MentionablesHandler().Handle(new ListMentionablesQuery(_workspaceId, space.Id), CancellationToken.None);

        result.Data!.CanBroadcast.Should().BeFalse();
    }

    [Fact]
    public async Task Mentionables_ForSomeoneNotInTheSpace_IsTheSameNotFoundAsAMissingSpace()
    {
        var space = Channel(SpaceKind.ChannelPrivate, "secret");
        AsUser(_carol);

        var privateSpace = () => MentionablesHandler().Handle(new ListMentionablesQuery(_workspaceId, space.Id), CancellationToken.None);
        var missing = () => MentionablesHandler().Handle(new ListMentionablesQuery(_workspaceId, Guid.NewGuid()), CancellationToken.None);

        await privateSpace.Should().ThrowAsync<AppNotFoundException>();
        await missing.Should().ThrowAsync<AppNotFoundException>();
    }

    // ── Conversations (direct and group) ─────────────────────────────────────

    private Space Group(params Guid[] others)
    {
        var space = Space.CreateGroup(_accountId, _workspaceId, "Team", others, _author, Now);
        _spaces.Add(space);
        return space;
    }

    private Space DirectWithBob()
    {
        var space = Space.CreateDirect(_accountId, _workspaceId, _author, _bob, Now);
        _spaces.Add(space);
        return space;
    }

    [Fact]
    public async Task Send_InAGroupConversation_StoresAMentionOfAParticipantAndCountsItForThem()
    {
        var space = Group(_bob, _carol);

        var result = await Send(space, "hi @Bob", [Person(3, 4, _bob)]);

        result.Data!.Message.Mentions.Should().ContainSingle(m => m.UserId == _bob);
        result.Data.Message.MentionNotices.Should().BeEmpty();
        (await MentionsOf(_bob, space)).Should().Be(1);
        (await MentionsOf(_carol, space)).Should().Be(0);
    }

    [Fact]
    public async Task Send_InAConversation_ToAWorkspaceMemberWhoIsNotAParticipant_WarnsAndNeverCountsIt()
    {
        var space = DirectWithBob();

        var result = await Send(space, "hi @Col", [Person(3, 4, _colleague)]);

        result.Data!.Message.MentionNotices.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new MentionNoticeDto(MentionResolver.CannotView, 3, 4, _colleague));
        AsUser(_colleague);
        (await Summary()).Data!.Spaces.Should().NotContain(s => s.SpaceId == space.Id);
    }

    [Fact]
    public async Task Send_ABroadcastInADirectConversation_HasNoAudienceButWorksInAGroup()
    {
        _broadcast.Setup(b => b.CanBroadcastAsync(_workspaceId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var direct = DirectWithBob();
        var group = Group(_bob, _carol);

        var inDirect = await Send(direct, "hey @channel", [Channel(4)], "c-d");
        var inGroup = await Send(group, "hey @channel", [Channel(4)], "c-g");

        inDirect.Data!.Message.Mentions.Should().BeEmpty();
        inDirect.Data.Message.MentionNotices.Single().Code.Should().Be(MentionResolver.NoAudience);
        inGroup.Data!.Message.Mentions.Should().ContainSingle();
    }

    [Fact]
    public async Task Mentionables_InAConversation_ListsTheParticipantsOnlyAndNoDirectoryBroadcast()
    {
        var group = Group(_bob, _carol);
        var direct = DirectWithBob();

        var inGroup = await MentionablesHandler().Handle(new ListMentionablesQuery(_workspaceId, group.Id), CancellationToken.None);
        var inDirect = await MentionablesHandler().Handle(new ListMentionablesQuery(_workspaceId, direct.Id), CancellationToken.None);

        inGroup.Data!.People.Should().OnlyContain(p => p.InSpace);
        inGroup.Data.People.Select(p => p.UserId).Should().BeEquivalentTo([_author, _bob, _carol]);
        inGroup.Data.CanBroadcast.Should().BeTrue();
        inDirect.Data!.People.Should().OnlyContain(p => p.InSpace);
        inDirect.Data.CanBroadcast.Should().BeFalse();
    }
}
