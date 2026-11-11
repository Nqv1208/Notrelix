using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Messages.Commands.DeleteMessage;
using Notrelix.Application.Features.Chat.Messages.Commands.SendMessage;
using Notrelix.Application.Features.Chat.Messages.Queries.ListMessages;
using Notrelix.Application.Features.Chat.Pins.Commands.PinMessage;
using Notrelix.Application.Features.Chat.Pins.Commands.UnpinMessage;
using Notrelix.Application.Features.Chat.Pins.Queries.ListPins;
using Notrelix.Application.Features.Chat.Messages.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Application.Tests.Features.Collaboration;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using AppValidationException = Notrelix.Application.Common.Exceptions.ValidationException;
using Space = Notrelix.Domain.Chat.Spaces.Space;
using AppConflictException = Notrelix.Application.Common.Exceptions.ConflictException;
using AppNotFoundException = Notrelix.Application.Common.Exceptions.NotFoundException;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.Chat.Pins;

/// <summary>
/// Pins (UC-CHT-F06, F07) and quote-reply (UC-CHT-F02) at handler level, with fakes. The row lock and the count under it are
/// proved against PostgreSQL in the integration tests; here the lock order is asserted through the fake allocator.
/// </summary>
public class PinHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);

    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private readonly Guid _outsider = Guid.NewGuid();

    private readonly Mock<ICurrentRequestContext> _context = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IWorkspaceMemberFacts> _members = new();
    private readonly List<Space> _spaces = [];
    private readonly List<Message> _messages = [];
    private readonly List<Reaction> _reactions = [];
    private readonly List<Pin> _pins = [];
    private readonly List<string> _order = [];
    private readonly Mock<IChatDbContext> _db = new();
    private readonly Mock<ISpaceSequenceAllocator> _sequences = new();
    private long _lastSequence;
    private int _clockTicks;

    public PinHandlerTests()
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
        _db.Setup(d => d.ChatPins).Returns(() => TestDbSet.Create(_pins).Object);

        _sequences.Setup(s => s.LockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback(() => _order.Add("lock"))
            .Returns(Task.CompletedTask);
        _sequences.Setup(s => s.NextAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => { _order.Add("next"); return ++_lastSequence; });

        _members.Setup(m => m.FindAsync(_workspaceId, _owner, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_owner, "Owner", true));
        _members.Setup(m => m.FindAsync(_workspaceId, _member, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_member, "Member", true));
        _members.Setup(m => m.FindAsync(_workspaceId, _outsider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_outsider, "Member", true));
    }

    private void AsUser(Guid userId) => _context.Setup(c => c.UserId).Returns(userId);

    private Space Channel(SpaceKind kind = SpaceKind.ChannelPublic)
    {
        var space = Space.CreateChannel(_accountId, _workspaceId, kind, "general", null, null, 1, _owner, Now);
        space.AddMember(_member, MembershipRole.Member, Now);
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

    private PinMessageCommandHandler PinHandler() =>
        new(_db.Object, _context.Object, _members.Object, _sequences.Object, new ChatSystemMessages(_db.Object, _sequences.Object, _clock.Object), _clock.Object, new InMemoryChatMessageAttachments());
    private UnpinMessageCommandHandler UnpinHandler() => new(_db.Object, _context.Object, _members.Object, _sequences.Object, _clock.Object);
    private ListPinsQueryHandler ListPinsHandler() => new(_db.Object, _context.Object, _members.Object, new InMemoryChatMessageAttachments());
    private ListMessagesQueryHandler ListMessagesHandler() => new(_db.Object, _context.Object, _members.Object, new InMemoryChatMessageAttachments());
    private DeleteMessageCommandHandler DeleteHandler() => new(_db.Object, _context.Object, _members.Object, _sequences.Object, _clock.Object, new InMemoryChatMessageAttachments());
    private SendMessageCommandHandler SendHandler() => new(_db.Object, _context.Object, _members.Object, _sequences.Object, _clock.Object, new InMemoryChatMessageAttachments());

    private int SystemMessageCount(Space space) => _messages.Count(m => m.SpaceId == space.Id && m.IsSystem);

    // ---- Pin (UC-CHT-F06) ----------------------------------------------------------------------

    [Fact]
    public async Task Pinning_stores_the_pin_announces_it_and_takes_the_lock_first()
    {
        var space = Channel();
        var message = Text(space);

        var result = await PinHandler().Handle(new PinMessageCommand(_workspaceId, message.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Created.Should().BeTrue();
        result.Data.Pin.Message.Id.Should().Be(message.Id);
        result.Data.Pin.PinnedBy.Should().Be(_member);
        _pins.Should().ContainSingle(p => p.MessageId == message.Id);
        SystemMessageCount(space).Should().Be(1, "the pin is announced in the channel with a system message");
        _order.Should().StartWith("lock");
    }

    [Fact]
    public async Task Pinning_twice_is_idempotent_and_announces_once()
    {
        var space = Channel();
        var message = Text(space);
        await PinHandler().Handle(new PinMessageCommand(_workspaceId, message.Id), CancellationToken.None);

        var again = await PinHandler().Handle(new PinMessageCommand(_workspaceId, message.Id), CancellationToken.None);

        again.Succeeded.Should().BeTrue();
        again.Data!.Created.Should().BeFalse();
        _pins.Should().ContainSingle();
        SystemMessageCount(space).Should().Be(1);
    }

    [Fact]
    public async Task A_system_message_cannot_be_pinned()
    {
        var space = Channel();
        var system = Message.CreateSystemMessage(_accountId, _workspaceId, space.Id, ++_lastSequence, _member, "joined the channel", Now);
        _messages.Add(system);

        var act = () => PinHandler().Handle(new PinMessageCommand(_workspaceId, system.Id), CancellationToken.None);

        var error = await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        error.Which.RuleCode.Should().Be(ChatRuleCodes.MessageNotPinnable);
        _pins.Should().BeEmpty();
    }

    [Fact]
    public async Task A_deleted_message_cannot_be_pinned()
    {
        var space = Channel();
        var message = Text(space);
        message.Delete(_member, Now.AddMinutes(1));

        var act = () => PinHandler().Handle(new PinMessageCommand(_workspaceId, message.Id), CancellationToken.None);

        var error = await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        error.Which.RuleCode.Should().Be(ChatRuleCodes.MessageDeleted);
        _pins.Should().BeEmpty();
    }

    [Fact]
    public async Task The_next_pin_past_the_limit_is_refused_with_limit_reached()
    {
        var space = Channel();
        for (var i = 0; i < ChatLimits.PinLimit; i++)
            _pins.Add(Pin.Create(_accountId, _workspaceId, space.Id, Text(space).Id, _member, Now));
        var message = Text(space);

        var act = () => PinHandler().Handle(new PinMessageCommand(_workspaceId, message.Id), CancellationToken.None);

        var error = await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>();
        error.Which.RuleCode.Should().Be(ChatRuleCodes.LimitReached);
        _pins.Should().HaveCount(ChatLimits.PinLimit);
    }

    [Fact]
    public async Task A_message_the_caller_is_not_a_member_of_is_not_found()
    {
        var space = Channel(SpaceKind.ChannelPrivate);
        var message = Text(space);
        AsUser(_outsider);

        var act = () => PinHandler().Handle(new PinMessageCommand(_workspaceId, message.Id), CancellationToken.None);

        await act.Should().ThrowAsync<AppNotFoundException>();
        _pins.Should().BeEmpty();
    }

    [Fact]
    public async Task Pinning_in_an_archived_channel_is_refused_with_channel_archived()
    {
        var space = Channel();
        var message = Text(space);
        space.SetArchived(true, _owner, Now);

        var act = () => PinHandler().Handle(new PinMessageCommand(_workspaceId, message.Id), CancellationToken.None);

        var error = await act.Should().ThrowAsync<AppConflictException>();
        error.Which.ErrorCode.Should().Be(ChatRuleCodes.ChannelArchived);
    }

    [Fact]
    public async Task A_message_in_another_workspace_is_the_same_not_found_as_a_missing_one()
    {
        var space = Channel();
        var message = Text(space);

        var act = () => PinHandler().Handle(new PinMessageCommand(Guid.NewGuid(), message.Id), CancellationToken.None);

        await act.Should().ThrowAsync<AppNotFoundException>();
    }

    // ---- Unpin (UC-CHT-F07) ---------------------------------------------------------------------

    [Fact]
    public async Task Unpinning_removes_the_pin_and_raises_the_fact()
    {
        var space = Channel();
        var message = Text(space);
        await PinHandler().Handle(new PinMessageCommand(_workspaceId, message.Id), CancellationToken.None);

        var result = await UnpinHandler().Handle(new UnpinMessageCommand(_workspaceId, message.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _pins.Should().BeEmpty();
    }

    [Fact]
    public async Task Unpinning_a_message_that_is_not_pinned_changes_nothing()
    {
        var space = Channel();
        var message = Text(space);

        var result = await UnpinHandler().Handle(new UnpinMessageCommand(_workspaceId, message.Id), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _pins.Should().BeEmpty();
    }

    // ---- List pins (UC-CHT-F07) -----------------------------------------------------------------

    [Fact]
    public async Task Pins_are_listed_newest_first_and_page_by_cursor()
    {
        var space = Channel();
        var ids = new List<Guid>();
        for (var i = 0; i < 5; i++)
        {
            var message = Text(space, $"message {i}");
            ids.Add(message.Id);
            await PinHandler().Handle(new PinMessageCommand(_workspaceId, message.Id), CancellationToken.None);
        }

        var first = await ListPinsHandler().Handle(new ListPinsQuery(_workspaceId, space.Id, null, 2), CancellationToken.None);
        first.Data!.Items.Select(p => p.Message.Id).Should().Equal(ids[4], ids[3]);
        first.Data.NextCursor.Should().NotBeNull();

        var second = await ListPinsHandler().Handle(new ListPinsQuery(_workspaceId, space.Id, first.Data.NextCursor, 2), CancellationToken.None);
        second.Data!.Items.Select(p => p.Message.Id).Should().Equal(ids[2], ids[1]);

        var last = await ListPinsHandler().Handle(new ListPinsQuery(_workspaceId, space.Id, second.Data!.NextCursor, 2), CancellationToken.None);
        last.Data!.Items.Select(p => p.Message.Id).Should().Equal(ids[0]);
        last.Data.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task A_private_channel_pin_list_is_not_found_for_a_non_member()
    {
        var space = Channel(SpaceKind.ChannelPrivate);
        var message = Text(space);
        _pins.Add(Pin.Create(_accountId, _workspaceId, space.Id, message.Id, _member, Now));
        AsUser(_outsider);

        var act = () => ListPinsHandler().Handle(new ListPinsQuery(_workspaceId, space.Id, null, 10), CancellationToken.None);

        await act.Should().ThrowAsync<AppNotFoundException>();
    }

    [Fact]
    public async Task A_malformed_cursor_is_refused()
    {
        var space = Channel();

        var act = () => ListPinsHandler().Handle(new ListPinsQuery(_workspaceId, space.Id, "not-a-cursor", 10), CancellationToken.None);

        await act.Should().ThrowAsync<AppValidationException>();
    }

    [Fact]
    public async Task Deleting_a_pinned_message_removes_its_pin()
    {
        var space = Channel();
        var message = Text(space);
        await PinHandler().Handle(new PinMessageCommand(_workspaceId, message.Id), CancellationToken.None);
        AsUser(_member);

        var result = await DeleteHandler().Handle(new DeleteMessageCommand(_workspaceId, message.Id, message.Version), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _pins.Should().BeEmpty("a pin ends when its message is deleted");
        _order.Should().Contain("lock", "the delete takes the space lock like a pin does");
    }

    // ---- Quote-reply (UC-CHT-F02) ---------------------------------------------------------------

    [Fact]
    public async Task A_quote_shows_the_live_content_of_the_quoted_message()
    {
        var space = Channel();
        var quoted = Text(space, "the original text");
        var reply = await SendHandler().Handle(new SendMessageCommand(_workspaceId, space.Id, "a reply", "q-1", quoted.Id), CancellationToken.None);

        reply.Succeeded.Should().BeTrue();
        reply.Data!.Message.ReplyToId.Should().Be(quoted.Id);
        reply.Data.Message.ReplyPreview.Should().NotBeNull();
        reply.Data.Message.ReplyPreview!.Content.Should().Be("the original text");
        reply.Data.Message.ReplyPreview.Deleted.Should().BeFalse();
    }

    [Fact]
    public async Task A_quote_of_a_deleted_message_is_a_tombstone_with_no_content()
    {
        var space = Channel();
        var quoted = Text(space, "secret wording");
        var reply = await SendHandler().Handle(new SendMessageCommand(_workspaceId, space.Id, "answer", "q-2", quoted.Id), CancellationToken.None);
        quoted.Delete(_member, Now.AddMinutes(2));

        var page = await ListMessagesHandler().Handle(new ListMessagesQuery(_workspaceId, space.Id, null, null, null, 50), CancellationToken.None);

        var quoting = page.Data!.Items.Single(m => m.Id == reply.Data!.Message.Id);
        quoting.ReplyPreview!.Deleted.Should().BeTrue();
        quoting.ReplyPreview.Content.Should().BeNull("no content of a deleted message leaks through a quote");
        quoting.ReplyPreview.AuthorId.Should().Be(_member, "the tombstone keeps its author and sequence");
    }

    [Fact]
    public async Task A_quote_is_refused_when_the_quoted_message_is_in_another_space()
    {
        var space = Channel();
        var other = Space.CreateChannel(_accountId, _workspaceId, SpaceKind.ChannelPublic, "other", null, null, 2, _owner, Now);
        other.AddMember(_member, MembershipRole.Member, Now);
        _spaces.Add(other);
        var elsewhere = Text(other, "not here");

        var act = () => SendHandler().Handle(new SendMessageCommand(_workspaceId, space.Id, "x", "q-3", elsewhere.Id), CancellationToken.None);

        await act.Should().ThrowAsync<AppNotFoundException>();
    }
}
