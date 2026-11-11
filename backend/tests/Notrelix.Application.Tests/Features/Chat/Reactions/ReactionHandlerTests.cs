using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Messages.Commands.DeleteMessage;
using Notrelix.Application.Features.Chat.Reactions.Commands.AddReaction;
using Notrelix.Application.Features.Chat.Reactions.Commands.RemoveReaction;
using Notrelix.Application.Features.Chat.Reactions.DTOs;
using Notrelix.Application.Features.Chat.Reactions.Queries.ListReactors;
using Notrelix.Application.Features.Chat.Reactions.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Application.Tests.Features.Collaboration;
using AppConflictException = Notrelix.Application.Common.Exceptions.ConflictException;
using AppNotFoundException = Notrelix.Application.Common.Exceptions.NotFoundException;
using AppValidationException = Notrelix.Application.Common.Exceptions.ValidationException;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Tests.Features.Chat.Reactions;

/// <summary>
/// Reactions (UC-CHT-G06, G07) at handler level, with fakes. The row lock and the read under it are proved against PostgreSQL in the
/// integration tests; here the lock order and the racing request are asserted through the fake allocator.
/// </summary>
public class ReactionHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private readonly Guid _otherMember = Guid.NewGuid();
    private readonly Guid _guest = Guid.NewGuid();
    private readonly Guid _outsider = Guid.NewGuid();

    private readonly Mock<ICurrentRequestContext> _context = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IWorkspaceMemberFacts> _members = new();
    private readonly List<Space> _spaces = [];
    private readonly List<Message> _messages = [];
    private readonly List<Reaction> _reactions = [];
    private readonly List<string> _order = [];
    private readonly Mock<IChatDbContext> _db = new();
    private readonly Mock<ISpaceSequenceAllocator> _sequences = new();
    private long _lastSequence;
    private int _clockTicks;

    public ReactionHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(() => Now.AddMinutes(++_clockTicks));
        _context.Setup(c => c.RequireAccountId()).Returns(_accountId);
        _context.Setup(c => c.RequireWorkspaceId()).Returns(_workspaceId);
        _context.Setup(c => c.IsAuthenticated).Returns(true);
        _context.Setup(c => c.UserId).Returns(_member);

        _db.Setup(d => d.ChatSpaces).Returns(() => TestDbSet.Create(_spaces).Object);
        _db.Setup(d => d.ChatMemberships).Returns(() => TestDbSet.Create(_spaces.SelectMany(s => s.Memberships).ToList()).Object);
        _db.Setup(d => d.ChatChannelGroups).Returns(TestDbSet.Create(new List<ChannelGroup>()).Object);
        _db.Setup(d => d.ChatMessages).Returns(() => TestDbSet.Create(_messages).Object);
        _db.Setup(d => d.ChatReactions).Returns(() => TestDbSet.Create(_reactions).Object);
        _db.Setup(d => d.ChatPins).Returns(TestDbSet.Create(new List<Pin>()).Object);
        _db.Setup(d => d.ChatThreads).Returns(TestDbSet.Create(new List<MessageThread>()).Object);
        _db.Setup(d => d.ChatThreadFollowers).Returns(TestDbSet.Create(new List<ThreadFollower>()).Object);

        _sequences.Setup(s => s.LockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback(() => _order.Add("lock"))
            .Returns(Task.CompletedTask);
        _sequences.Setup(s => s.NextAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => ++_lastSequence);

        _members.Setup(m => m.FindAsync(_workspaceId, _owner, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_owner, "Owner", true));
        _members.Setup(m => m.FindAsync(_workspaceId, _member, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_member, "Member", true));
        _members.Setup(m => m.FindAsync(_workspaceId, _otherMember, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_otherMember, "Member", true));
        _members.Setup(m => m.FindAsync(_workspaceId, _guest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_guest, "Guest", true));
        _members.Setup(m => m.FindAsync(_workspaceId, _outsider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_outsider, "Member", true));
    }

    private void AsUser(Guid userId) => _context.Setup(c => c.UserId).Returns(userId);

    private Space Channel(SpaceKind kind = SpaceKind.ChannelPublic)
    {
        var space = Space.CreateChannel(_accountId, _workspaceId, kind, "general", null, null, 1, _owner, Now);
        space.AddMember(_member, MembershipRole.Member, Now);
        space.AddMember(_otherMember, MembershipRole.Member, Now);
        _spaces.Add(space);
        return space;
    }

    private Message Text(Space space, string text = "a message", Guid? author = null)
    {
        var message = Message.CreateUserMessage(_accountId, _workspaceId, space.Id, ++_lastSequence, author ?? _member,
            text, $"c-{Guid.NewGuid():N}", null, Now);
        _messages.Add(message);
        return message;
    }

    private Message SystemMessage(Space space)
    {
        var message = Message.CreateSystemMessage(_accountId, _workspaceId, space.Id, ++_lastSequence, _owner, "A pin was added.", Now);
        _messages.Add(message);
        return message;
    }

    private Reaction Existing(Message message, Guid user, string kind)
    {
        var reaction = Reaction.Create(_accountId, _workspaceId, message.SpaceId, message.Id, user, kind, Now.AddSeconds(_reactions.Count));
        _reactions.Add(reaction);
        return reaction;
    }

    private AddReactionCommandHandler AddHandler() => new(_db.Object, _context.Object, _members.Object, _sequences.Object, _clock.Object);
    private RemoveReactionCommandHandler RemoveHandler() => new(_db.Object, _context.Object, _members.Object, _sequences.Object, _clock.Object);
    private ListReactorsQueryHandler ListHandler() => new(_db.Object, _context.Object, _members.Object);
    private DeleteMessageCommandHandler DeleteHandler() => new(_db.Object, _context.Object, _members.Object, _sequences.Object, _clock.Object);

    private static ReactionSummaryDto Summary(MessageReactionsDto dto, string kind) =>
        dto.Reactions.Single(r => r.Emoji == kind);

    // ---- Add (UC-CHT-G06) ---------------------------------------------------------------------

    [Fact]
    public async Task Reacting_with_a_palette_kind_stores_it_and_returns_the_summary_as_created()
    {
        var space = Channel();
        var message = Text(space);

        var result = await AddHandler().Handle(new AddReactionCommand(_workspaceId, message.Id, "👍"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Created.Should().BeTrue();
        _reactions.Should().ContainSingle(r => r.MessageId == message.Id && r.UserId == _member && r.Kind == "👍");
        var summary = Summary(result.Data.Message, "👍");
        summary.Count.Should().Be(1);
        summary.ReactedByMe.Should().BeTrue();
    }

    [Fact]
    public async Task Reacting_twice_with_the_same_kind_is_a_no_op_and_stores_one_reaction()
    {
        var space = Channel();
        var message = Text(space);
        await AddHandler().Handle(new AddReactionCommand(_workspaceId, message.Id, "🎉"), CancellationToken.None);

        var again = await AddHandler().Handle(new AddReactionCommand(_workspaceId, message.Id, "🎉"), CancellationToken.None);

        again.Succeeded.Should().BeTrue();
        again.Data!.Created.Should().BeFalse("a repeat is 200 with no change");
        _reactions.Should().ContainSingle();
        Summary(again.Data.Message, "🎉").Count.Should().Be(1);
    }

    [Fact]
    public async Task A_racing_request_for_the_same_reaction_is_a_no_op_once_the_lock_is_taken()
    {
        var space = Channel();
        var message = Text(space);
        // The other request commits its insert while this one waits for the space lock; the read under the lock sees it.
        _sequences.Setup(s => s.LockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                _order.Add("lock");
                if (_reactions.Count == 0) Existing(message, _member, "🚀");
            })
            .Returns(Task.CompletedTask);

        var result = await AddHandler().Handle(new AddReactionCommand(_workspaceId, message.Id, "🚀"), CancellationToken.None);

        result.Data!.Created.Should().BeFalse();
        _reactions.Should().ContainSingle();
        _order.Should().StartWith("lock");
    }

    [Fact]
    public async Task Two_people_reacting_with_one_kind_count_two_and_only_the_caller_is_marked()
    {
        var space = Channel();
        var message = Text(space);
        Existing(message, _otherMember, "👀");

        var result = await AddHandler().Handle(new AddReactionCommand(_workspaceId, message.Id, "👀"), CancellationToken.None);

        var summary = Summary(result.Data!.Message, "👀");
        summary.Count.Should().Be(2);
        summary.ReactedByMe.Should().BeTrue("the caller is one of the two");
    }

    [Fact]
    public void Reacting_with_a_kind_off_the_palette_is_refused_by_the_validator()
    {
        var validation = new AddReactionCommandValidator().Validate(new AddReactionCommand(_workspaceId, Guid.NewGuid(), "🔥"));

        validation.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Reacting_to_a_system_message_is_refused_with_message_not_reactable()
    {
        var space = Channel();
        var system = SystemMessage(space);

        var act = () => AddHandler().Handle(new AddReactionCommand(_workspaceId, system.Id, "👍"), CancellationToken.None);

        var error = await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        error.Which.RuleCode.Should().Be(ChatRuleCodes.MessageNotReactable);
        _reactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Reacting_to_a_deleted_message_is_refused_with_message_deleted()
    {
        var space = Channel();
        var message = Text(space);
        message.Delete(_member, Now.AddMinutes(1));

        var act = () => AddHandler().Handle(new AddReactionCommand(_workspaceId, message.Id, "👍"), CancellationToken.None);

        var error = await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        error.Which.RuleCode.Should().Be(ChatRuleCodes.MessageDeleted);
        _reactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Reacting_in_an_archived_channel_is_refused_with_channel_archived()
    {
        var space = Channel();
        var message = Text(space);
        space.SetArchived(true, _owner, Now);

        var act = () => AddHandler().Handle(new AddReactionCommand(_workspaceId, message.Id, "👍"), CancellationToken.None);

        var error = await act.Should().ThrowAsync<AppConflictException>();
        error.Which.ErrorCode.Should().Be(ChatRuleCodes.ChannelArchived);
        _reactions.Should().BeEmpty();
    }

    [Fact]
    public async Task A_message_in_a_private_channel_the_caller_is_not_in_is_not_found()
    {
        var space = Channel(SpaceKind.ChannelPrivate);
        var message = Text(space);
        AsUser(_outsider);

        var act = () => AddHandler().Handle(new AddReactionCommand(_workspaceId, message.Id, "👍"), CancellationToken.None);

        await act.Should().ThrowAsync<AppNotFoundException>();
        _reactions.Should().BeEmpty();
    }

    [Fact]
    public async Task A_guest_not_added_to_the_channel_is_not_found()
    {
        var space = Channel();
        var message = Text(space);
        AsUser(_guest);

        var act = () => AddHandler().Handle(new AddReactionCommand(_workspaceId, message.Id, "👍"), CancellationToken.None);

        await act.Should().ThrowAsync<AppNotFoundException>();
        _reactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Reacting_takes_the_space_lock_before_the_read_and_the_insert()
    {
        var space = Channel();
        var message = Text(space);

        await AddHandler().Handle(new AddReactionCommand(_workspaceId, message.Id, "🎉"), CancellationToken.None);

        _order.Should().StartWith("lock");
    }

    // ---- Remove (UC-CHT-G07) ------------------------------------------------------------------

    [Fact]
    public async Task Removing_the_callers_reaction_drops_the_count_and_clears_the_mark()
    {
        var space = Channel();
        var message = Text(space);
        Existing(message, _member, "❤️");
        Existing(message, _otherMember, "❤️");

        var result = await RemoveHandler().Handle(new RemoveReactionCommand(_workspaceId, message.Id, "❤️"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _reactions.Should().ContainSingle(r => r.UserId == _otherMember);
        var summary = Summary(result.Data!, "❤️");
        summary.Count.Should().Be(1);
        summary.ReactedByMe.Should().BeFalse();
    }

    [Fact]
    public async Task Removing_the_last_reaction_of_a_kind_drops_the_kind_from_the_summary()
    {
        var space = Channel();
        var message = Text(space);
        Existing(message, _member, "🚀");

        var result = await RemoveHandler().Handle(new RemoveReactionCommand(_workspaceId, message.Id, "🚀"), CancellationToken.None);

        result.Data!.Reactions.Should().BeEmpty();
        _reactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Removing_a_reaction_the_caller_does_not_have_is_a_no_op_that_succeeds()
    {
        var space = Channel();
        var message = Text(space);
        Existing(message, _otherMember, "👍");

        var result = await RemoveHandler().Handle(new RemoveReactionCommand(_workspaceId, message.Id, "👍"), CancellationToken.None);

        result.Succeeded.Should().BeTrue("removing a missing reaction is 200");
        _reactions.Should().ContainSingle(r => r.UserId == _otherMember);
        Summary(result.Data!, "👍").Count.Should().Be(1);
    }

    [Fact]
    public async Task Removing_in_an_archived_channel_is_refused_with_channel_archived()
    {
        var space = Channel();
        var message = Text(space);
        Existing(message, _member, "👍");
        space.SetArchived(true, _owner, Now);

        var act = () => RemoveHandler().Handle(new RemoveReactionCommand(_workspaceId, message.Id, "👍"), CancellationToken.None);

        var error = await act.Should().ThrowAsync<AppConflictException>();
        error.Which.ErrorCode.Should().Be(ChatRuleCodes.ChannelArchived);
        _reactions.Should().ContainSingle();
    }

    [Fact]
    public async Task Removing_from_a_deleted_message_changes_nothing_and_succeeds()
    {
        var space = Channel();
        var message = Text(space);
        message.Delete(_member, Now.AddMinutes(1));

        var result = await RemoveHandler().Handle(new RemoveReactionCommand(_workspaceId, message.Id, "👍"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Reactions.Should().BeEmpty();
    }

    [Fact]
    public void Removing_a_reaction_of_a_kind_off_the_palette_is_refused_by_the_validator()
    {
        var validation = new RemoveReactionCommandValidator().Validate(new RemoveReactionCommand(_workspaceId, Guid.NewGuid(), "🔥"));

        validation.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Removing_is_refused_for_a_private_channel_the_caller_is_not_in()
    {
        var space = Channel(SpaceKind.ChannelPrivate);
        var message = Text(space);
        Existing(message, _otherMember, "👍");
        AsUser(_outsider);

        var act = () => RemoveHandler().Handle(new RemoveReactionCommand(_workspaceId, message.Id, "👍"), CancellationToken.None);

        await act.Should().ThrowAsync<AppNotFoundException>();
        _reactions.Should().ContainSingle();
    }

    // ---- Who reacted (UC-CHT-G07) -------------------------------------------------------------

    [Fact]
    public async Task Listing_reactors_returns_the_earliest_reaction_first_with_its_emoji_and_person()
    {
        var space = Channel();
        var message = Text(space);
        var first = Existing(message, _otherMember, "👍");
        var second = Existing(message, _member, "🎉");

        var result = await ListHandler().Handle(new ListReactorsQuery(_workspaceId, message.Id, null, null, 50), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Items.Select(r => (r.Emoji, r.UserId)).Should().Equal(("👍", _otherMember), ("🎉", _member));
        result.Data.NextCursor.Should().BeNull();
        first.CreatedAt.Should().BeBefore(second.CreatedAt);
    }

    [Fact]
    public async Task Listing_reactors_narrows_to_one_kind_when_asked()
    {
        var space = Channel();
        var message = Text(space);
        Existing(message, _otherMember, "👍");
        Existing(message, _member, "🎉");

        var result = await ListHandler().Handle(new ListReactorsQuery(_workspaceId, message.Id, "🎉", null, 50), CancellationToken.None);

        result.Data!.Items.Should().ContainSingle().Which.Emoji.Should().Be("🎉");
    }

    [Fact]
    public async Task Listing_reactors_pages_with_a_cursor_without_repeats_or_gaps()
    {
        var space = Channel();
        var message = Text(space);
        Existing(message, _otherMember, "👍");
        Existing(message, _member, "🎉");
        Existing(message, _owner, "👀");

        var firstPage = await ListHandler().Handle(new ListReactorsQuery(_workspaceId, message.Id, null, null, 2), CancellationToken.None);
        var secondPage = await ListHandler().Handle(new ListReactorsQuery(_workspaceId, message.Id, null, firstPage.Data!.NextCursor, 2), CancellationToken.None);

        firstPage.Data!.Items.Should().HaveCount(2);
        firstPage.Data.NextCursor.Should().NotBeNull();
        secondPage.Data!.Items.Should().ContainSingle().Which.Emoji.Should().Be("👀");
        secondPage.Data.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task Listing_reactors_refuses_a_malformed_cursor()
    {
        var space = Channel();
        var message = Text(space);

        var act = () => ListHandler().Handle(new ListReactorsQuery(_workspaceId, message.Id, null, "not-a-cursor", 50), CancellationToken.None);

        await act.Should().ThrowAsync<AppValidationException>();
    }

    [Fact]
    public async Task Listing_reactors_of_a_private_channel_the_caller_is_not_in_is_not_found()
    {
        var space = Channel(SpaceKind.ChannelPrivate);
        var message = Text(space);
        Existing(message, _otherMember, "👍");
        AsUser(_outsider);

        var act = () => ListHandler().Handle(new ListReactorsQuery(_workspaceId, message.Id, null, null, 50), CancellationToken.None);

        await act.Should().ThrowAsync<AppNotFoundException>();
    }

    [Fact]
    public async Task Listing_reactors_of_a_missing_message_is_not_found()
    {
        Channel();

        var act = () => ListHandler().Handle(new ListReactorsQuery(_workspaceId, Guid.NewGuid(), null, null, 50), CancellationToken.None);

        await act.Should().ThrowAsync<AppNotFoundException>();
    }

    // ---- Deletion and the summary (UC-CHT-B05, G06) -------------------------------------------

    [Fact]
    public async Task Deleting_a_message_clears_its_reactions_without_a_reaction_fact_of_their_own()
    {
        var space = Channel();
        var message = Text(space);
        Existing(message, _otherMember, "👍");
        Existing(message, _member, "🎉");

        await DeleteHandler().Handle(new DeleteMessageCommand(_workspaceId, message.Id, message.Version), CancellationToken.None);

        _reactions.Should().BeEmpty();
    }

    [Fact]
    public void The_summary_applies_one_change_in_palette_order_without_a_second_read()
    {
        var current = new List<ReactionSummaryDto> { new("🎉", 1, false), new("👍", 2, true) };

        var added = ReactionSummaries.Apply(current, "👀", added: true);
        var removed = ReactionSummaries.Apply(current, "👍", added: false);

        added.Select(r => r.Emoji).Should().Equal("👍", "🎉", "👀");
        added.Single(r => r.Emoji == "👀").Should().Be(new ReactionSummaryDto("👀", 1, true));
        removed.Single(r => r.Emoji == "👍").Should().Be(new ReactionSummaryDto("👍", 1, false));
    }

    [Fact]
    public void The_palette_matches_the_collaboration_palette()
    {
        Notrelix.Domain.Chat.Messages.ReactionKinds.Palette
            .Should().Equal(Notrelix.Application.Features.Collaboration.Reactions.ReactionPalette.Emojis);
    }
}
