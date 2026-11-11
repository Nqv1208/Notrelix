using Notrelix.Application.Common.Realtime;
using FluentValidation.TestHelper;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Channels.Commands.UpdateChannel;
using Notrelix.Application.Features.Chat.Channels.Commands.ArchiveChannel;
using Notrelix.Application.Features.Chat.Members.Commands.JoinChannel;
using Notrelix.Application.Features.Chat.Members.Commands.RemoveChannelMember;
using Notrelix.Application.Features.Chat.Messages.Commands.DeleteMessage;
using Notrelix.Application.Features.Chat.Messages.Commands.EditMessage;
using Notrelix.Application.Features.Chat.Messages.Commands.SendMessage;
using Notrelix.Application.Features.Chat.Messages.Queries.ListMessages;
using Notrelix.Application.Features.Chat.Messages.Services;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Application.Tests.Features.Collaboration;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Space = Notrelix.Domain.Chat.Spaces.Space;
using Notrelix.Testing.Application.Fakes;

namespace Notrelix.Application.Tests.Features.Chat;

/// <summary>Message use cases UC-CHT-B01 to B05 and the system announcements of B09, at handler level with fakes.</summary>
public class MessageHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private readonly Guid _guest = Guid.NewGuid();
    private readonly Guid _outsider = Guid.NewGuid();

    private readonly Mock<ICurrentRequestContext> _context = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Mock<IWorkspaceMemberFacts> _members = new();
    private readonly List<Space> _spaces = [];
    private readonly List<Membership> _memberships = [];
    private readonly List<Message> _messages = [];
    private readonly List<Reaction> _reactions = [];
    private readonly List<MessageMention> _mentions = [];
    private readonly Mock<Notrelix.Application.Features.Chat.Mentions.Services.IBroadcastAuthorizer> _broadcast = new();
    private readonly Mock<IChatDbContext> _db = new();
    private readonly Mock<ISpaceSequenceAllocator> _sequences = new();
    private readonly Mock<IAccessChangeSignal> _access = new();
    private long _lastSequence;

    public MessageHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(() => Now);
        _context.Setup(c => c.RequireAccountId()).Returns(_accountId);
        _context.Setup(c => c.RequireWorkspaceId()).Returns(_workspaceId);
        _context.Setup(c => c.IsAuthenticated).Returns(true);

        _db.Setup(d => d.ChatSpaces).Returns(TestDbSet.Create(_spaces).Object);
        // Read on every access, so a membership that changes after the handler started (a concurrent leave) shows up like it would in the database.
        _db.Setup(d => d.ChatMemberships).Returns(() => TestDbSet.Create(_memberships.Concat(_spaces.SelectMany(s => s.Memberships)).ToList()).Object);
        _db.Setup(d => d.ChatChannelGroups).Returns(TestDbSet.Create(new List<ChannelGroup>()).Object);
        _db.Setup(d => d.ChatMessages).Returns(TestDbSet.Create(_messages).Object);
        _db.Setup(d => d.ChatReactions).Returns(() => TestDbSet.Create(_reactions).Object);
        _db.Setup(d => d.ChatMessageMentions).Returns(() => TestDbSet.Create(_mentions).Object);
        _db.Setup(d => d.ChatPins).Returns(TestDbSet.Create(new List<Pin>()).Object);

        _sequences.Setup(s => s.LockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _sequences.Setup(s => s.NextAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(() => ++_lastSequence);

        AsCaller(_owner, "Owner");
        _members.Setup(m => m.FindAsync(_workspaceId, _member, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_member, "Member", true));
        _members.Setup(m => m.FindAsync(_workspaceId, _guest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_guest, "Guest", true));
        _members.Setup(m => m.FindAsync(_workspaceId, _outsider, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_outsider, "Member", true));
    }

    private void AsCaller(Guid userId, string role)
    {
        _context.Setup(c => c.UserId).Returns(userId);
        _members.Setup(m => m.FindAsync(_workspaceId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(userId, role, true));
    }

    private void AsUser(Guid userId) => _context.Setup(c => c.UserId).Returns(userId);

    private Space Channel(SpaceKind kind = SpaceKind.ChannelPublic, string name = "general")
    {
        var space = Space.CreateChannel(_accountId, _workspaceId, kind, name, null, null, 1, _owner, Now);
        _spaces.Add(space);
        return space;
    }

    /// <summary>Archives after membership is set up: an archived channel takes no new member.</summary>
    private static Space Archived(Space space)
    {
        space.SetArchived(true, space.CreatedBy!.Value, Now);
        return space;
    }

    private Message Seed(Space space, long sequence, Guid author, string content = "text", bool deleted = false)
    {
        var message = Message.CreateUserMessage(_accountId, _workspaceId, space.Id, sequence, author, content, $"c-{sequence}-{Guid.NewGuid():N}", null, Now.AddHours(-1));
        if (deleted) message.Delete(author, Now.AddMinutes(-30));
        _messages.Add(message);
        _lastSequence = Math.Max(_lastSequence, sequence);
        return message;
    }

    private SendMessageCommandHandler SendHandler() => new(_db.Object, _context.Object, _members.Object, _sequences.Object, _clock.Object, _broadcast.Object, new InMemoryChatMessageAttachments());
    private EditMessageCommandHandler EditHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object, _broadcast.Object, new InMemoryChatMessageAttachments());
    private DeleteMessageCommandHandler DeleteHandler() => new(_db.Object, _context.Object, _members.Object, _sequences.Object, _clock.Object, new InMemoryChatMessageAttachments());
    private ListMessagesQueryHandler ListHandler() => new(_db.Object, _context.Object, _members.Object, new InMemoryChatMessageAttachments());
    private ChatSystemMessages SystemMessages() => new(_db.Object, _sequences.Object, _clock.Object);
    private JoinChannelCommandHandler JoinHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object, SystemMessages());
    private RemoveChannelMemberCommandHandler RemoveHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object, SystemMessages(), _access.Object);
    private UpdateChannelCommandHandler UpdateHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object, SystemMessages());
    private ArchiveChannelCommandHandler ArchiveHandler() => new(_db.Object, _context.Object, _members.Object, _clock.Object, SystemMessages(), _access.Object);

    private static SendMessageCommand Send(Guid workspaceId, Guid spaceId, string content, string clientMessageId = "client-1") =>
        new(workspaceId, spaceId, content, clientMessageId, null);

    // ── Send (UC-CHT-B01, B06; CHT-005, CHT-006) ─────────────────────────────

    [Fact]
    public async Task Send_ByAMember_StoresTheMessageWithTheNextSequence()
    {
        var space = Channel();
        space.AddMember(_member, MembershipRole.Member, Now);
        AsUser(_member);

        var result = await SendHandler().Handle(Send(_workspaceId, space.Id, "  hello  "), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Created.Should().BeTrue();
        result.Data.Message.Sequence.Should().Be(1);
        result.Data.Message.Content.Should().Be("hello");
        result.Data.Message.AuthorId.Should().Be(_member);
        _messages.Should().ContainSingle(m => m.SpaceId == space.Id && m.Sequence == 1);
        _sequences.Verify(s => s.LockAsync(space.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Send_WithTheSameClientMessageIdAgain_ReturnsTheStoredMessageAndStoresNothing()
    {
        var space = Channel();
        space.AddMember(_member, MembershipRole.Member, Now);
        AsUser(_member);
        var first = await SendHandler().Handle(Send(_workspaceId, space.Id, "once"), CancellationToken.None);

        var second = await SendHandler().Handle(Send(_workspaceId, space.Id, "once again"), CancellationToken.None);

        second.Succeeded.Should().BeTrue();
        second.Data!.Created.Should().BeFalse();
        second.Data.Message.Id.Should().Be(first.Data!.Message.Id);
        second.Data.Message.Content.Should().Be("once");
        _messages.Should().HaveCount(1);
        _sequences.Verify(s => s.NextAsync(space.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Send_ToAnArchivedChannel_IsA409ChannelArchived()
    {
        var space = Channel();
        space.AddMember(_member, MembershipRole.Member, Now);
        Archived(space);
        AsUser(_member);

        var act = () => SendHandler().Handle(Send(_workspaceId, space.Id, "late"), CancellationToken.None);

        var error = await act.Should().ThrowAsync<ConflictException>();
        error.Which.ErrorCode.Should().Be(ChatRuleCodes.ChannelArchived);
        _messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Send_WhenTheMemberLeavesWhileWaitingForTheLock_IsNotFoundAndStoresNothing()
    {
        var space = Channel();
        space.AddMember(_member, MembershipRole.Member, Now);
        AsUser(_member);
        // A leave that held the lock first commits while this send waits for it.
        _sequences.Setup(s => s.LockAsync(space.Id, It.IsAny<CancellationToken>()))
            .Returns(() => { space.RemoveMember(_member, Now); return Task.CompletedTask; });

        var act = () => SendHandler().Handle(Send(_workspaceId, space.Id, "too late"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Send_WhenTheChannelIsArchivedWhileWaitingForTheLock_IsA409AndStoresNothing()
    {
        var space = Channel();
        space.AddMember(_member, MembershipRole.Member, Now);
        AsUser(_member);
        _sequences.Setup(s => s.LockAsync(space.Id, It.IsAny<CancellationToken>()))
            .Returns(() => { space.SetArchived(true, _owner, Now); return Task.CompletedTask; });

        var act = () => SendHandler().Handle(Send(_workspaceId, space.Id, "too late"), CancellationToken.None);

        var error = await act.Should().ThrowAsync<ConflictException>();
        error.Which.ErrorCode.Should().Be(ChatRuleCodes.ChannelArchived);
        _messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Send_FromANonMemberOfAPublicChannel_IsNotFound()
    {
        var space = Channel();
        AsUser(_outsider);

        var act = () => SendHandler().Handle(Send(_workspaceId, space.Id, "hi"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        _messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Send_ToAPrivateChannelOfAnotherPerson_IsTheSameNotFoundAsAMissingChannel()
    {
        var secret = Channel(SpaceKind.ChannelPrivate, "secret");
        AsUser(_outsider);

        var missing = () => SendHandler().Handle(Send(_workspaceId, Guid.NewGuid(), "hi"), CancellationToken.None);
        var notMember = () => SendHandler().Handle(Send(_workspaceId, secret.Id, "hi"), CancellationToken.None);

        await missing.Should().ThrowAsync<NotFoundException>();
        await notMember.Should().ThrowAsync<NotFoundException>();
        _messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Send_ToAChannelOfAnotherWorkspace_IsNotFound()
    {
        var space = Channel();
        space.AddMember(_member, MembershipRole.Member, Now);
        AsUser(_member);
        var otherWorkspace = Guid.NewGuid();
        _members.Setup(m => m.FindAsync(otherWorkspace, _member, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkspaceMemberFact(_member, "Member", true));

        var act = () => SendHandler().Handle(Send(otherWorkspace, space.Id, "hi"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Send_ReplyToAMessageOfAnotherSpace_IsNotFound()
    {
        var space = Channel();
        var other = Channel(name: "other");
        space.AddMember(_member, MembershipRole.Member, Now);
        AsUser(_member);
        var quoted = Seed(other, 1, _member);

        var act = () => SendHandler().Handle(
            new SendMessageCommand(_workspaceId, space.Id, "re", "client-q", quoted.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void Send_ValidatesTheContentAndTheClientMessageId()
    {
        var validator = new SendMessageCommandValidator();

        validator.TestValidate(Send(_workspaceId, Guid.NewGuid(), "   ")).ShouldHaveValidationErrorFor(x => x.Content);
        validator.TestValidate(Send(_workspaceId, Guid.NewGuid(), new string('a', ChatLimits.MessageMaxLength + 1))).ShouldHaveValidationErrorFor(x => x.Content);
        validator.TestValidate(Send(_workspaceId, Guid.NewGuid(), new string('a', ChatLimits.MessageMaxLength))).ShouldNotHaveValidationErrorFor(x => x.Content);
        validator.TestValidate(Send(_workspaceId, Guid.NewGuid(), "ok", "")).ShouldHaveValidationErrorFor(x => x.ClientMessageId);
    }

    // ── Edit (UC-CHT-B04) ───────────────────────────────────────────────────

    [Fact]
    public async Task Edit_ByTheAuthor_ReplacesTheTextAndKeepsTheSequence()
    {
        var space = Channel();
        space.AddMember(_member, MembershipRole.Member, Now);
        var message = Seed(space, 4, _member, "first");
        AsUser(_member);

        var result = await EditHandler().Handle(new EditMessageCommand(_workspaceId, message.Id, "second", 1), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Data!.Content.Should().Be("second");
        result.Data.Edited.Should().BeTrue();
        result.Data.Sequence.Should().Be(4);
        result.Data.Version.Should().Be(2);
    }

    [Fact]
    public async Task Edit_ByAnotherMember_IsForbidden()
    {
        var space = Channel();
        space.AddMember(_member, MembershipRole.Member, Now);
        var message = Seed(space, 1, _owner);
        AsUser(_member);

        var act = () => EditHandler().Handle(new EditMessageCommand(_workspaceId, message.Id, "mine now", 1), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Edit_ByANonMemberOfAPrivateChannel_IsNotFound()
    {
        var secret = Channel(SpaceKind.ChannelPrivate, "secret");
        var message = Seed(secret, 1, _owner);
        AsUser(_outsider);

        var act = () => EditHandler().Handle(new EditMessageCommand(_workspaceId, message.Id, "x", 1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Edit_InAnArchivedChannel_IsA409()
    {
        var space = Channel();
        space.AddMember(_member, MembershipRole.Member, Now);
        var message = Seed(space, 1, _member);
        Archived(space);
        AsUser(_member);

        var act = () => EditHandler().Handle(new EditMessageCommand(_workspaceId, message.Id, "x", 1), CancellationToken.None);

        var error = await act.Should().ThrowAsync<ConflictException>();
        error.Which.ErrorCode.Should().Be(ChatRuleCodes.ChannelArchived);
    }

    [Fact]
    public void Edit_ValidatesTheVersionAndTheContent()
    {
        var validator = new EditMessageCommandValidator();

        validator.TestValidate(new EditMessageCommand(_workspaceId, Guid.NewGuid(), "x", 0)).ShouldHaveValidationErrorFor(x => x.ExpectedVersion);
        validator.TestValidate(new EditMessageCommand(_workspaceId, Guid.NewGuid(), "", 1)).ShouldHaveValidationErrorFor(x => x.Content);
    }

    // ── Delete (UC-CHT-B05) ─────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ByTheAuthor_LeavesATombstoneInTheList()
    {
        var space = Channel();
        space.AddMember(_member, MembershipRole.Member, Now);
        var message = Seed(space, 2, _member, "gone soon");
        AsUser(_member);

        var result = await DeleteHandler().Handle(new DeleteMessageCommand(_workspaceId, message.Id, 1), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        message.IsDeleted.Should().BeTrue();
        message.Content.Should().BeNull();
        message.AuthorId.Should().Be(_member);
        message.Sequence.Should().Be(2);
    }

    [Fact]
    public async Task Delete_OfAnExistingTombstone_SucceedsAndChangesNothing()
    {
        var space = Channel();
        space.AddMember(_member, MembershipRole.Member, Now);
        var message = Seed(space, 2, _member, deleted: true);
        AsUser(_member);

        var result = await DeleteHandler().Handle(new DeleteMessageCommand(_workspaceId, message.Id, 2), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        message.Version.Should().Be(2);
    }

    [Fact]
    public async Task Delete_ByAnotherMember_IsForbidden()
    {
        var space = Channel();
        space.AddMember(_member, MembershipRole.Member, Now);
        var message = Seed(space, 1, _owner);
        AsUser(_member);

        var act = () => DeleteHandler().Handle(new DeleteMessageCommand(_workspaceId, message.Id, 1), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        message.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_OfAMissingMessage_IsNotFound()
    {
        AsUser(_owner);

        var act = () => DeleteHandler().Handle(new DeleteMessageCommand(_workspaceId, Guid.NewGuid(), 1), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── List (UC-CHT-B02, B03) ──────────────────────────────────────────────

    private static ListMessagesQuery Page(Guid workspaceId, Guid spaceId, long? before = null, long? after = null, long? around = null, int limit = 50) =>
        new(workspaceId, spaceId, before, after, around, limit);

    private Space SpaceWithMessages(int count, bool addSecondTombstone = true)
    {
        var space = Channel();
        space.AddMember(_member, MembershipRole.Member, Now);
        for (var sequence = 1; sequence <= count; sequence++)
            Seed(space, sequence, _member, $"m{sequence}", deleted: addSecondTombstone && sequence == 3);
        AsUser(_member);
        return space;
    }

    [Fact]
    public async Task List_WithNoCursor_ReturnsTheNewestPageInAscendingOrder()
    {
        var space = SpaceWithMessages(120, addSecondTombstone: false);

        var result = await ListHandler().Handle(Page(_workspaceId, space.Id), CancellationToken.None);

        var items = result.Data!.Items;
        items.Should().HaveCount(50);
        items.Select(i => i.Sequence).Should().BeInAscendingOrder();
        items.First().Sequence.Should().Be(71);
        items.Last().Sequence.Should().Be(120);
        result.Data.HasOlder.Should().BeTrue();
        result.Data.HasNewer.Should().BeFalse();
    }

    [Fact]
    public async Task List_Before_ReturnsOlderMessagesWithoutRepeatingTheCursor()
    {
        var space = SpaceWithMessages(120, addSecondTombstone: false);

        var result = await ListHandler().Handle(Page(_workspaceId, space.Id, before: 71), CancellationToken.None);

        var sequences = result.Data!.Items.Select(i => i.Sequence).ToList();
        sequences.Should().BeInAscendingOrder();
        sequences.First().Should().Be(21);
        sequences.Last().Should().Be(70);
        result.Data.HasOlder.Should().BeTrue();
        result.Data.HasNewer.Should().BeTrue();
    }

    [Fact]
    public async Task List_After_ReturnsNewerMessagesAndSaysWhenThereAreMore()
    {
        var space = SpaceWithMessages(20, addSecondTombstone: false);

        var result = await ListHandler().Handle(Page(_workspaceId, space.Id, after: 5, limit: 10), CancellationToken.None);

        result.Data!.Items.Select(i => i.Sequence).Should().Equal(6, 7, 8, 9, 10, 11, 12, 13, 14, 15);
        result.Data.HasOlder.Should().BeTrue();
        result.Data.HasNewer.Should().BeTrue();
    }

    [Fact]
    public async Task List_After_TheLastMessage_IsAnEmptyPageWithNothingNewer()
    {
        var space = SpaceWithMessages(20, addSecondTombstone: false);

        var result = await ListHandler().Handle(Page(_workspaceId, space.Id, after: 20), CancellationToken.None);

        result.Data!.Items.Should().BeEmpty();
        result.Data.HasNewer.Should().BeFalse();
        result.Data.HasOlder.Should().BeTrue();
    }

    [Fact]
    public async Task List_Around_ReturnsAWindowCenteredOnTheSequence()
    {
        var space = SpaceWithMessages(100, addSecondTombstone: false);

        var result = await ListHandler().Handle(Page(_workspaceId, space.Id, around: 60, limit: 10), CancellationToken.None);

        result.Data!.Items.Select(i => i.Sequence).Should().Equal(56, 57, 58, 59, 60, 61, 62, 63, 64, 65);
        result.Data.HasOlder.Should().BeTrue();
        result.Data.HasNewer.Should().BeTrue();
    }

    [Fact]
    public async Task List_IncludesTombstonesWithNullContentAndTheirSequence()
    {
        var space = SpaceWithMessages(5);

        var result = await ListHandler().Handle(Page(_workspaceId, space.Id), CancellationToken.None);

        var tombstone = result.Data!.Items.Should().ContainSingle(i => i.Sequence == 3).Subject;
        tombstone.Deleted.Should().BeTrue();
        tombstone.Content.Should().BeNull();
        tombstone.AuthorId.Should().Be(_member);
        result.Data.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task List_OfAnEmptySpace_IsAnEmptyPage()
    {
        var space = Channel();
        AsUser(_owner);

        var result = await ListHandler().Handle(Page(_workspaceId, space.Id), CancellationToken.None);

        result.Data!.Items.Should().BeEmpty();
        result.Data.HasOlder.Should().BeFalse();
        result.Data.HasNewer.Should().BeFalse();
    }

    [Fact]
    public async Task List_OfAPrivateChannelByANonMember_IsNotFound()
    {
        var secret = Channel(SpaceKind.ChannelPrivate, "secret");
        Seed(secret, 1, _owner);
        AsUser(_outsider);

        var act = () => ListHandler().Handle(Page(_workspaceId, secret.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task List_OfAGuestWhoIsNotInThePublicChannel_IsNotFound()
    {
        var space = Channel();
        Seed(space, 1, _owner);
        AsUser(_guest);

        var act = () => ListHandler().Handle(Page(_workspaceId, space.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void List_ValidatesTheLimitAndTheCursors()
    {
        var validator = new ListMessagesQueryValidator();
        var space = Guid.NewGuid();

        validator.TestValidate(Page(_workspaceId, space, limit: 0)).ShouldHaveValidationErrorFor(x => x.Limit);
        validator.TestValidate(Page(_workspaceId, space, limit: 101)).ShouldHaveValidationErrorFor(x => x.Limit);
        validator.TestValidate(Page(_workspaceId, space, limit: 100)).ShouldNotHaveValidationErrorFor(x => x.Limit);
        validator.TestValidate(Page(_workspaceId, space, before: 5, after: 3)).ShouldHaveValidationErrorFor(x => x);
        validator.TestValidate(Page(_workspaceId, space, before: 0)).ShouldHaveValidationErrorFor(x => x.Before);
    }

    // ── System announcements (UC-CHT-B09) ───────────────────────────────────

    private IReadOnlyList<Message> SystemMessagesIn(Space space) =>
        _messages.Where(m => m.SpaceId == space.Id && m.IsSystem).ToList();

    [Fact]
    public async Task Rename_AnnouncesTheNewNameAsASystemMessageAttributedToTheActor()
    {
        var space = Channel(name: "old");
        AsUser(_owner);

        var result = await UpdateHandler().Handle(
            new UpdateChannelCommand(_workspaceId, space.Id, "new", null, space.Version), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        var announced = SystemMessagesIn(space).Should().ContainSingle().Subject;
        announced.AuthorId.Should().Be(_owner);
        announced.Content.Should().Be("renamed the channel to new");
    }

    [Fact]
    public async Task Rename_ToTheSameName_AnnouncesNothing()
    {
        var space = Channel(name: "same");
        AsUser(_owner);

        await UpdateHandler().Handle(new UpdateChannelCommand(_workspaceId, space.Id, "same", "a topic", space.Version), CancellationToken.None);

        SystemMessagesIn(space).Should().BeEmpty();
    }

    [Fact]
    public async Task Archive_AndUnarchive_EachAnnounceOnce()
    {
        var space = Channel();
        AsUser(_owner);

        await ArchiveHandler().Handle(new ArchiveChannelCommand(_workspaceId, space.Id, true, space.Version), CancellationToken.None);
        await ArchiveHandler().Handle(new ArchiveChannelCommand(_workspaceId, space.Id, false, space.Version), CancellationToken.None);

        SystemMessagesIn(space).Select(m => m.Content).Should().Equal("archived the channel", "unarchived the channel");
    }

    [Fact]
    public async Task Join_AnnouncesAJoinOnlyWhenTheMemberIsNew()
    {
        var space = Channel();
        AsUser(_member);

        await JoinHandler().Handle(new JoinChannelCommand(_workspaceId, space.Id), CancellationToken.None);
        await JoinHandler().Handle(new JoinChannelCommand(_workspaceId, space.Id), CancellationToken.None);

        var joined = SystemMessagesIn(space).Should().ContainSingle().Subject;
        joined.AuthorId.Should().Be(_member);
        joined.Content.Should().Be("joined the channel");
    }

    [Fact]
    public async Task Leave_AnnouncesALeaveAttributedToTheLeaver()
    {
        var space = Channel();
        space.AddMember(_member, MembershipRole.Member, Now);
        AsUser(_member);

        await RemoveHandler().Handle(new RemoveChannelMemberCommand(_workspaceId, space.Id, _member), CancellationToken.None);

        var left = SystemMessagesIn(space).Should().ContainSingle().Subject;
        left.AuthorId.Should().Be(_member);
        left.Content.Should().Be("left the channel");
    }

    [Fact]
    public async Task Leave_ClosesTheLeaversLiveSubscriptionToTheChannel()
    {
        // UC-CHT-C06: the access signal names the person and the channel, so the gateway closes that subscription at once.
        var space = Channel();
        space.AddMember(_member, MembershipRole.Member, Now);
        AsUser(_member);

        await RemoveHandler().Handle(new RemoveChannelMemberCommand(_workspaceId, space.Id, _member), CancellationToken.None);

        _access.Verify(a => a.ChannelPerson(_workspaceId, space.Id, _member), Times.Once);
    }

    [Fact]
    public async Task Removal_ClosesTheRemovedPersonsSubscriptionNotEveryones()
    {
        var space = Channel();
        space.AddMember(_member, MembershipRole.Member, Now);
        AsUser(_owner);

        await RemoveHandler().Handle(new RemoveChannelMemberCommand(_workspaceId, space.Id, _member), CancellationToken.None);

        _access.Verify(a => a.ChannelPerson(_workspaceId, space.Id, _member), Times.Once);
        _access.Verify(a => a.Channel(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Archive_ClosesEverySubscriptionToTheChannel_AndUnarchiveClosesNothing()
    {
        var space = Channel();
        AsUser(_owner);

        await ArchiveHandler().Handle(new ArchiveChannelCommand(_workspaceId, space.Id, true, space.Version), CancellationToken.None);
        _access.Verify(a => a.Channel(_workspaceId, space.Id), Times.Once);

        await ArchiveHandler().Handle(new ArchiveChannelCommand(_workspaceId, space.Id, false, space.Version), CancellationToken.None);
        _access.Verify(a => a.Channel(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task Removal_AnnouncesARemovalAttributedToTheManager()
    {
        var space = Channel();
        space.AddMember(_member, MembershipRole.Member, Now);
        AsUser(_owner);

        await RemoveHandler().Handle(new RemoveChannelMemberCommand(_workspaceId, space.Id, _member), CancellationToken.None);

        var removed = SystemMessagesIn(space).Should().ContainSingle().Subject;
        removed.AuthorId.Should().Be(_owner);
        removed.Content.Should().Be("was removed from the channel");
    }

    [Fact]
    public async Task SystemMessages_CannotBeEditedOrDeletedByTheirActor()
    {
        var space = Channel(name: "old");
        AsUser(_owner);
        await UpdateHandler().Handle(new UpdateChannelCommand(_workspaceId, space.Id, "new", null, space.Version), CancellationToken.None);
        var announced = SystemMessagesIn(space).Single();

        var edit = () => EditHandler().Handle(new EditMessageCommand(_workspaceId, announced.Id, "tampered", 1), CancellationToken.None);
        var delete = () => DeleteHandler().Handle(new DeleteMessageCommand(_workspaceId, announced.Id, 1), CancellationToken.None);

        (await edit.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(ChatRuleCodes.SystemMessageImmutable);
        (await delete.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(ChatRuleCodes.SystemMessageImmutable);
    }
}
