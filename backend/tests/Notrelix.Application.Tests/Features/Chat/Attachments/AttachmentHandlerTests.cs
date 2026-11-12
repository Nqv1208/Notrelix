using Notrelix.Domain.Chat.Permissions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notrelix.Application.Common.Storage;
using Notrelix.Application.Features.Chat.Abstractions;
using Notrelix.Application.Features.Chat.Attachments.Commands.DeleteMessageAttachment;
using Notrelix.Application.Features.Chat.Attachments.Commands.UploadMessageAttachment;
using Notrelix.Application.Features.Chat.Attachments.Queries.GetMessageAttachmentContent;
using Notrelix.Application.Features.Chat.Attachments.Queries.ListMessageAttachments;
using Notrelix.Application.Features.Chat.Messages.Commands.DeleteMessage;
using Notrelix.Application.Features.Chat.Messages.Queries.ListMessages;
using Notrelix.Application.Features.Collaboration.Abstractions;
using Notrelix.Application.Features.Collaboration.Attachments;
using Notrelix.Application.Features.Collaboration.Attachments.Services;
using Notrelix.Application.Features.Collaboration.Public.Attachments;
using Notrelix.Application.Features.Workspaces.Public.Members;
using Notrelix.Application.Tests.Features.Collaboration;
using Notrelix.Domain.Chat.Messages;
using Notrelix.Domain.Chat.Rules;
using Notrelix.Domain.Chat.Spaces;
using Notrelix.Domain.Collaboration.Attachments;
using Notrelix.Testing.Application.Fakes;
using AppConflictException = Notrelix.Application.Common.Exceptions.ConflictException;
using AppForbiddenException = Notrelix.Application.Common.Exceptions.ForbiddenException;
using AppNotFoundException = Notrelix.Application.Common.Exceptions.NotFoundException;
using PayloadTooLargeException = Notrelix.Application.Common.Exceptions.PayloadTooLargeException;
using UnsupportedMediaTypeException = Notrelix.Application.Common.Exceptions.UnsupportedMediaTypeException;
using Space = Notrelix.Domain.Chat.Spaces.Space;

namespace Notrelix.Application.Tests.Features.Chat.Attachments;

/// <summary>
/// Files on a message (UC-CHT-G08, CHT-012) at handler level. Chat's own rules (who may attach, archive, tombstone, delete clears) run
/// against an in-memory Collaboration boundary; the shared file policy (blocked type, size cap, per-message cap) runs through the real
/// Collaboration flow with in-memory storage, so the message target goes through the same code as board items and pages.
/// </summary>
public class AttachmentHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 11, 9, 0, 0, TimeSpan.Zero);
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];

    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _owner = Guid.NewGuid();
    private readonly Guid _member = Guid.NewGuid();
    private readonly Guid _otherMember = Guid.NewGuid();
    private readonly Guid _guest = Guid.NewGuid();
    private readonly Guid _outsider = Guid.NewGuid();

    private readonly Mock<ICurrentRequestContext> _context = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Notrelix.Application.Tests.Features.Chat.RateLimits.FakeChatRateLimiter _limits = new();
    private readonly Mock<IWorkspaceMemberFacts> _members = new();
    private readonly List<Space> _spaces = [];
    private readonly List<Message> _messages = [];
    private readonly List<Attachment> _rows = [];
    private readonly Mock<IChatDbContext> _db = new();
    private readonly Mock<ISpaceSequenceAllocator> _sequences = new();
    private readonly InMemoryChatMessageAttachments _files = new();
    private readonly MemoryStorage _storage = new();
    private readonly RequestRollbackCompensations _compensations = new(NullLogger<RequestRollbackCompensations>.Instance);
    private long _lastSequence;

    public AttachmentHandlerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(Now);
        _context.Setup(c => c.RequireAccountId()).Returns(_accountId);
        _context.Setup(c => c.RequireWorkspaceId()).Returns(_workspaceId);
        _context.Setup(c => c.IsAuthenticated).Returns(true);
        _context.Setup(c => c.UserId).Returns(_member);

        _db.Setup(d => d.ChatSpaces).Returns(() => TestDbSet.Create(_spaces).Object);
        _db.Setup(d => d.ChatMemberships).Returns(() => TestDbSet.Create(_spaces.SelectMany(s => s.Memberships).ToList()).Object);
        _db.Setup(d => d.ChatPermissionOverrides).Returns(() => TestDbSet.Create(_spaces.SelectMany(s => s.PermissionOverrides).ToList()).Object);
        _db.Setup(d => d.ChatChannelGroups).Returns(TestDbSet.Create(new List<ChannelGroup>()).Object);
        _db.Setup(d => d.ChatMessages).Returns(() => TestDbSet.Create(_messages).Object);
        _db.Setup(d => d.ChatMessageMentions).Returns(TestDbSet.Create(new List<MessageMention>()).Object);
        _db.Setup(d => d.ChatReactions).Returns(TestDbSet.Create(new List<Reaction>()).Object);
        _db.Setup(d => d.ChatPins).Returns(TestDbSet.Create(new List<Pin>()).Object);
        _db.Setup(d => d.ChatThreads).Returns(TestDbSet.Create(new List<MessageThread>()).Object);
        _db.Setup(d => d.ChatThreadFollowers).Returns(TestDbSet.Create(new List<ThreadFollower>()).Object);

        _sequences.Setup(s => s.LockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        foreach (var (user, role) in new[] { (_owner, "Owner"), (_member, "Member"), (_otherMember, "Member"), (_guest, "Guest"), (_outsider, "Member") })
        {
            var id = user;
            var name = role;
            _members.Setup(m => m.FindAsync(_workspaceId, id, It.IsAny<CancellationToken>())).ReturnsAsync(new WorkspaceMemberFact(id, name, true));
        }
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

    private Message Text(Space space, Guid? author = null)
    {
        var message = Message.CreateUserMessage(_accountId, _workspaceId, space.Id, ++_lastSequence, author ?? _member, "a message", $"c-{Guid.NewGuid():N}", null, Now);
        _messages.Add(message);
        return message;
    }

    private IChatMessageAttachments RealFlow(AttachmentUploadOptions? options = null)
    {
        var collab = new Mock<ICollaborationDbContext>();
        collab.Setup(c => c.Attachments).Returns(TestDbSet.Create(_rows).Object);
        return new ChatMessageAttachments(
            collab.Object, _context.Object, _clock.Object, _storage, _compensations, new InMemoryAttachmentTargetLock(),
            Options.Create(options ?? new AttachmentUploadOptions()), NullLogger<ChatMessageAttachments>.Instance);
    }

    private UploadMessageAttachmentCommandHandler UploadHandler(IChatMessageAttachments? attachments = null) =>
        new(_db.Object, _context.Object, _members.Object, _sequences.Object, attachments ?? _files, _clock.Object, _limits);

    private Task<Notrelix.Application.Common.Models.Result<Notrelix.Application.Features.Chat.Attachments.DTOs.MessageAttachmentDto>> Upload(
        Message message, string name = "report.pdf", byte[]? bytes = null, IChatMessageAttachments? attachments = null) =>
        UploadHandler(attachments).Handle(
            new UploadMessageAttachmentCommand(_workspaceId, message.Id, name, new MemoryStream(bytes ?? Png)), CancellationToken.None);

    // ---- Upload ------------------------------------------------------------------------------

    [Fact]
    public async Task The_author_attaches_a_file_and_gets_its_metadata_and_a_download_path()
    {
        var message = Text(Channel());

        var result = await Upload(message);

        result.Succeeded.Should().BeTrue();
        result.Data!.FileName.Should().Be("report.pdf");
        result.Data.SizeBytes.Should().Be(Png.Length);
        result.Data.MessageId.Should().Be(message.Id);
        result.Data.DownloadPath.Should().Be($"/api/v1/workspaces/{_workspaceId}/chat/messages/{message.Id}/attachments/{result.Data.Id}/content");
        _files.Files.Should().ContainSingle();
    }

    [Fact]
    public async Task A_stored_file_goes_through_the_shared_flow_under_the_chat_message_target()
    {
        var message = Text(Channel());

        var result = await Upload(message, "photo.png", attachments: RealFlow());

        result.Succeeded.Should().BeTrue();
        var row = _rows.Should().ContainSingle().Subject;
        row.Target.Kind.Value.Should().Be("chat.message");
        row.Target.ResourceId.Should().Be(message.Id);
        row.IsStoredFile.Should().BeTrue();
        row.Metadata.StorageKey.Should().StartWith($"{_accountId:N}/{_workspaceId:N}/attachments/");
        row.Metadata.ContentType.Should().Be("image/png");
        _storage.Objects[row.Metadata.StorageKey!].Should().Equal(Png);
    }

    [Fact]
    public async Task A_blocked_extension_is_refused_before_any_byte_is_stored()
    {
        var message = Text(Channel());

        var act = () => Upload(message, "setup.exe", attachments: RealFlow());

        await act.Should().ThrowAsync<UnsupportedMediaTypeException>();
        _rows.Should().BeEmpty();
        _storage.Objects.Should().BeEmpty();
    }

    [Fact]
    public async Task An_executable_disguised_by_its_name_is_refused_by_its_content()
    {
        var message = Text(Channel());

        var act = () => Upload(message, "notes.txt", [0x4D, 0x5A, 0x90, 0x00, 1, 2, 3, 4], RealFlow());

        await act.Should().ThrowAsync<UnsupportedMediaTypeException>();
        _rows.Should().BeEmpty();
    }

    [Fact]
    public async Task A_file_over_the_size_cap_is_refused_and_leaves_no_row()
    {
        var message = Text(Channel());
        var flow = RealFlow(new AttachmentUploadOptions { MaxFileSizeBytes = 8 });

        var act = () => Upload(message, "big.png", Png, flow);

        await act.Should().ThrowAsync<PayloadTooLargeException>();
        _rows.Should().BeEmpty();
    }

    [Fact]
    public async Task A_message_holds_no_more_files_than_the_per_message_cap()
    {
        var message = Text(Channel());
        var flow = RealFlow(new AttachmentUploadOptions { MaxAttachmentsPerMessage = 1 });
        (await Upload(message, "a.png", attachments: flow)).Succeeded.Should().BeTrue();

        var act = () => Upload(message, "b.png", attachments: flow);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>())
            .Which.RuleCode.Should().Be(Notrelix.Domain.Collaboration.CollaborationRuleCodes.Collaboration_Attachment_MaxAttachmentsExceeded);
        _rows.Should().ContainSingle();
    }

    [Fact]
    public async Task A_person_outside_the_space_gets_the_same_not_found_as_a_missing_message()
    {
        var message = Text(Channel(SpaceKind.ChannelPrivate), author: _member);
        AsUser(_outsider);

        var act = () => Upload(message);

        await act.Should().ThrowAsync<AppNotFoundException>();
        _files.UploadCalls.Should().Be(0);
    }

    [Fact]
    public async Task A_member_cannot_attach_to_another_persons_message()
    {
        var message = Text(Channel(), author: _otherMember);

        var act = () => Upload(message);

        await act.Should().ThrowAsync<AppForbiddenException>();
        _files.UploadCalls.Should().Be(0);
    }

    [Fact]
    public async Task A_guest_added_to_the_space_attaches_to_their_own_message()
    {
        var space = Channel();
        space.AddMember(_guest, MembershipRole.Member, Now);
        var message = Text(space, author: _guest);
        AsUser(_guest);

        var result = await Upload(message);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task A_guest_not_added_to_the_space_gets_not_found()
    {
        var space = Channel();
        var message = Text(space, author: _guest);
        AsUser(_guest);

        var act = () => Upload(message);

        await act.Should().ThrowAsync<AppNotFoundException>();
    }

    [Fact]
    public async Task An_archived_channel_refuses_a_new_file()
    {
        var space = Channel();
        var message = Text(space);
        space.SetArchived(true, _owner, Now);

        var act = () => Upload(message);

        (await act.Should().ThrowAsync<AppConflictException>()).Which.ErrorCode.Should().Be(ChatRuleCodes.ChannelArchived);
        _files.UploadCalls.Should().Be(0);
    }

    [Fact]
    public async Task A_channel_archived_while_the_bytes_streamed_refuses_the_upload_under_the_lock()
    {
        var space = Channel();
        var message = Text(space);
        _sequences.Setup(s => s.LockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback(() => space.SetArchived(true, _owner, Now))
            .Returns(Task.CompletedTask);

        var act = () => Upload(message);

        (await act.Should().ThrowAsync<AppConflictException>()).Which.ErrorCode.Should().Be(ChatRuleCodes.ChannelArchived);
    }

    [Fact]
    public async Task A_member_who_left_while_the_bytes_streamed_cannot_finish_the_upload()
    {
        var space = Channel();
        var message = Text(space);
        _sequences.Setup(s => s.LockAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback(() => space.RemoveMember(_member, Now))
            .Returns(Task.CompletedTask);

        var act = () => Upload(message);

        await act.Should().ThrowAsync<AppNotFoundException>();
    }

    [Fact]
    public async Task A_deleted_message_cannot_get_a_file()
    {
        var space = Channel();
        var message = Text(space);
        message.Delete(_member, Now);

        var act = () => Upload(message);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>()).Which.RuleCode.Should().Be(ChatRuleCodes.MessageDeleted);
    }

    [Fact]
    public async Task A_system_message_cannot_get_a_file()
    {
        var space = Channel();
        var system = Message.CreateSystemMessage(_accountId, _workspaceId, space.Id, ++_lastSequence, _member, "A pin was added.", Now);
        _messages.Add(system);

        var act = () => Upload(system);

        (await act.Should().ThrowAsync<Notrelix.Domain.Common.Exceptions.BusinessRuleException>()).Which.RuleCode.Should().Be(ChatRuleCodes.MessageNotAttachable);
    }

    // ---- Read and download -------------------------------------------------------------------

    [Fact]
    public async Task A_reader_lists_the_files_of_a_message_with_a_download_path()
    {
        var space = Channel();
        var message = Text(space);
        _files.Seed(message.Id, "a.pdf");
        _files.Seed(message.Id, "b.pdf");
        AsUser(_otherMember);

        var result = await new ListMessageAttachmentsQueryHandler(_db.Object, _context.Object, _members.Object, _files)
            .Handle(new ListMessageAttachmentsQuery(_workspaceId, message.Id), CancellationToken.None);

        result.Data!.Select(a => a.FileName).Should().Equal("a.pdf", "b.pdf");
        result.Data.Should().OnlyContain(a => a.DownloadPath.Contains($"/messages/{message.Id}/attachments/{a.Id}/content"));
    }

    [Fact]
    public async Task Only_a_reader_of_the_space_downloads_a_file()
    {
        var space = Channel(SpaceKind.ChannelPrivate);
        var message = Text(space);
        var file = _files.Seed(message.Id);
        var handler = new GetMessageAttachmentContentQueryHandler(_db.Object, _context.Object, _members.Object, _files);

        AsUser(_otherMember);
        (await handler.Handle(new GetMessageAttachmentContentQuery(_workspaceId, message.Id, file.Id), CancellationToken.None))
            .Data!.FileName.Should().Be("report.pdf");

        AsUser(_outsider);
        var act = () => handler.Handle(new GetMessageAttachmentContentQuery(_workspaceId, message.Id, file.Id), CancellationToken.None);
        await act.Should().ThrowAsync<AppNotFoundException>();
    }

    [Fact]
    public async Task An_attachment_of_another_message_is_not_found_through_this_one()
    {
        var space = Channel();
        var mine = Text(space);
        var other = Text(space);
        var foreign = _files.Seed(other.Id);
        var handler = new GetMessageAttachmentContentQueryHandler(_db.Object, _context.Object, _members.Object, _files);

        var act = () => handler.Handle(new GetMessageAttachmentContentQuery(_workspaceId, mine.Id, foreign.Id), CancellationToken.None);

        await act.Should().ThrowAsync<AppNotFoundException>();
    }

    [Fact]
    public async Task A_stored_file_of_the_message_is_downloaded_through_the_real_flow_and_a_board_item_file_is_not()
    {
        var space = Channel();
        var message = Text(space);
        var flow = RealFlow();
        var stored = (await Upload(message, "photo.png", attachments: flow)).Data!;
        var handler = new GetMessageAttachmentContentQueryHandler(_db.Object, _context.Object, _members.Object, flow);

        var opened = await handler.Handle(new GetMessageAttachmentContentQuery(_workspaceId, message.Id, stored.Id), CancellationToken.None);
        using var bytes = new MemoryStream();
        await opened.Data!.Content.CopyToAsync(bytes);
        bytes.ToArray().Should().Equal(Png);

        // A file of another target kind with the same id is not reachable through the message.
        var itemRow = Attachment.CreateStoredFile(
            _accountId, _workspaceId, ResourceRef.Create(AttachmentTargets.BoardItem, message.Id, _workspaceId), AttachmentType.Image,
            "item.png", Png.Length, "image/png", $"{_accountId:N}/{_workspaceId:N}/attachments/other", 1024, 0, 50, _member, Now);
        _rows.Add(itemRow);
        var foreign = () => handler.Handle(new GetMessageAttachmentContentQuery(_workspaceId, message.Id, itemRow.Id), CancellationToken.None);
        await foreign.Should().ThrowAsync<AppNotFoundException>();
    }

    [Fact]
    public async Task A_page_of_messages_carries_the_file_lists_in_one_query()
    {
        var space = Channel();
        var first = Text(space);
        var second = Text(space);
        var third = Text(space);
        _files.Seed(first.Id, "one.pdf");
        _files.Seed(third.Id, "three.pdf");
        _files.Seed(third.Id, "four.pdf");
        AsUser(_otherMember);

        var page = await new ListMessagesQueryHandler(_db.Object, _context.Object, _members.Object, _files)
            .Handle(new ListMessagesQuery(_workspaceId, space.Id, null, null, null, 50), CancellationToken.None);

        var items = page.Data!.Items;
        items.Single(m => m.Id == first.Id).Attachments.Select(a => a.FileName).Should().Equal("one.pdf");
        items.Single(m => m.Id == second.Id).Attachments.Should().BeEmpty();
        items.Single(m => m.Id == third.Id).Attachments.Should().HaveCount(2);
        _files.ListCalls.Should().Be(1, "the whole page is one attachment query, not one per message");
    }

    // ---- Realtime fact -----------------------------------------------------------------------

    private static List<Notrelix.Domain.Chat.Messages.Events.MessageAttachmentChangedDomainEvent> Facts(Message message) =>
        message.DomainEvents.OfType<Notrelix.Domain.Chat.Messages.Events.MessageAttachmentChangedDomainEvent>().ToList();

    [Fact]
    public async Task An_upload_raises_one_added_fact_with_the_file_metadata_and_leaves_the_message_version()
    {
        var message = Text(Channel());
        ((Notrelix.Domain.Common.IHasDomainEvents)message).ClearDomainEvents();
        var version = message.Version;

        var result = await Upload(message);

        var fact = Facts(message).Should().ContainSingle().Subject;
        fact.Added.Should().BeTrue();
        fact.AttachmentId.Should().Be(result.Data!.Id);
        fact.FileName.Should().Be("report.pdf");
        fact.SpaceId.Should().Be(message.SpaceId);
        fact.ActorId.Should().Be(_member);
        message.Version.Should().Be(version, "a file does not change the message text or version");
    }

    [Fact]
    public async Task A_refused_upload_raises_no_fact()
    {
        var message = Text(Channel());
        ((Notrelix.Domain.Common.IHasDomainEvents)message).ClearDomainEvents();

        var act = () => Upload(message, "run.exe", attachments: RealFlow());

        await act.Should().ThrowAsync<UnsupportedMediaTypeException>();
        Facts(message).Should().BeEmpty();
    }

    [Fact]
    public async Task A_removal_raises_one_removed_fact_with_only_the_file_id()
    {
        var space = Channel();
        var message = Text(space);
        var file = _files.Seed(message.Id);
        ((Notrelix.Domain.Common.IHasDomainEvents)message).ClearDomainEvents();

        await new DeleteMessageAttachmentCommandHandler(_db.Object, _context.Object, _members.Object, _files, _clock.Object)
            .Handle(new DeleteMessageAttachmentCommand(_workspaceId, message.Id, file.Id), CancellationToken.None);

        var fact = Facts(message).Should().ContainSingle().Subject;
        fact.Added.Should().BeFalse();
        fact.AttachmentId.Should().Be(file.Id);
        fact.FileName.Should().BeNull();
    }

    [Fact]
    public async Task A_removal_of_a_file_that_is_not_on_the_message_raises_no_fact()
    {
        var message = Text(Channel());
        ((Notrelix.Domain.Common.IHasDomainEvents)message).ClearDomainEvents();

        var act = () => new DeleteMessageAttachmentCommandHandler(_db.Object, _context.Object, _members.Object, _files, _clock.Object)
            .Handle(new DeleteMessageAttachmentCommand(_workspaceId, message.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<AppNotFoundException>();
        Facts(message).Should().BeEmpty();
    }

    // ---- Remove and delete -------------------------------------------------------------------

    [Fact]
    public async Task The_author_removes_a_file_and_another_member_cannot()
    {
        var space = Channel();
        var message = Text(space);
        var file = _files.Seed(message.Id);
        var handler = new DeleteMessageAttachmentCommandHandler(_db.Object, _context.Object, _members.Object, _files, _clock.Object);

        AsUser(_otherMember);
        var denied = () => handler.Handle(new DeleteMessageAttachmentCommand(_workspaceId, message.Id, file.Id), CancellationToken.None);
        await denied.Should().ThrowAsync<AppForbiddenException>();
        _files.Files.Should().ContainSingle();

        AsUser(_member);
        (await handler.Handle(new DeleteMessageAttachmentCommand(_workspaceId, message.Id, file.Id), CancellationToken.None)).Succeeded.Should().BeTrue();
        _files.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task Removing_a_file_that_is_not_on_this_message_is_not_found()
    {
        var space = Channel();
        var message = Text(space);
        var handler = new DeleteMessageAttachmentCommandHandler(_db.Object, _context.Object, _members.Object, _files, _clock.Object);

        var act = () => handler.Handle(new DeleteMessageAttachmentCommand(_workspaceId, message.Id, Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<AppNotFoundException>();
    }

    [Fact]
    public async Task An_archived_channel_refuses_to_remove_a_file()
    {
        var space = Channel();
        var message = Text(space);
        var file = _files.Seed(message.Id);
        space.SetArchived(true, _owner, Now);

        var act = () => new DeleteMessageAttachmentCommandHandler(_db.Object, _context.Object, _members.Object, _files, _clock.Object)
            .Handle(new DeleteMessageAttachmentCommand(_workspaceId, message.Id, file.Id), CancellationToken.None);

        (await act.Should().ThrowAsync<AppConflictException>()).Which.ErrorCode.Should().Be(ChatRuleCodes.ChannelArchived);
        _files.Files.Should().ContainSingle();
    }

    [Fact]
    public async Task Deleting_a_message_removes_its_files_and_leaves_a_tombstone_with_none()
    {
        var space = Channel();
        var message = Text(space);
        var kept = Text(space);
        _files.Seed(message.Id);
        _files.Seed(message.Id);
        _files.Seed(kept.Id);

        var result = await new DeleteMessageCommandHandler(_db.Object, _context.Object, _members.Object, _sequences.Object, _clock.Object, _files)
            .Handle(new DeleteMessageCommand(_workspaceId, message.Id, message.Version), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        _files.Files.Should().ContainSingle().Which.MessageId.Should().Be(kept.Id);

        var page = await new ListMessagesQueryHandler(_db.Object, _context.Object, _members.Object, _files)
            .Handle(new ListMessagesQuery(_workspaceId, space.Id, null, null, null, 50), CancellationToken.None);
        page.Data!.Items.Single(m => m.Id == message.Id).Attachments.Should().BeEmpty();
    }

    [Fact]
    public async Task A_deleted_message_has_no_downloadable_file()
    {
        var space = Channel();
        var message = Text(space);
        var file = _files.Seed(message.Id);
        message.Delete(_member, Now);

        var act = () => new GetMessageAttachmentContentQueryHandler(_db.Object, _context.Object, _members.Object, _files)
            .Handle(new GetMessageAttachmentContentQuery(_workspaceId, message.Id, file.Id), CancellationToken.None);

        await act.Should().ThrowAsync<AppNotFoundException>();
    }

    [Fact]
    public async Task The_real_flow_removes_every_file_of_the_message_and_only_that_message()
    {
        var space = Channel();
        var message = Text(space);
        var other = Text(space);
        var flow = RealFlow();
        await Upload(message, "a.png", attachments: flow);
        await Upload(message, "b.png", attachments: flow);
        await Upload(other, "c.png", attachments: flow);

        var removed = await flow.RemoveAllAsync(message.Id, CancellationToken.None);

        removed.Should().Be(2);
        _rows.Count(r => r.IsDeleted).Should().Be(2);
        _rows.Single(r => !r.IsDeleted).Target.ResourceId.Should().Be(other.Id);
    }

    // ---- Validators --------------------------------------------------------------------------

    [Fact]
    public void The_upload_validator_requires_a_message_a_name_and_a_body()
    {
        var validator = new UploadMessageAttachmentCommandValidator();

        validator.Validate(new UploadMessageAttachmentCommand(_workspaceId, Guid.Empty, "a.png", new MemoryStream())).IsValid.Should().BeFalse();
        validator.Validate(new UploadMessageAttachmentCommand(_workspaceId, Guid.NewGuid(), "", new MemoryStream())).IsValid.Should().BeFalse();
        validator.Validate(new UploadMessageAttachmentCommand(_workspaceId, Guid.NewGuid(), "a.png", new MemoryStream())).IsValid.Should().BeTrue();
    }

    private sealed class MemoryStorage : IStorageService
    {
        public Dictionary<string, byte[]> Objects { get; } = new();

        public async Task WriteAsync(string key, Stream content, string contentType, CancellationToken ct)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, ct);
            Objects[key] = buffer.ToArray();
        }

        public Task<Stream?> OpenReadAsync(string key, CancellationToken ct) =>
            Task.FromResult<Stream?>(Objects.TryGetValue(key, out var bytes) ? new MemoryStream(bytes) : null);

        public Task DeleteAsync(string key, CancellationToken ct)
        {
            Objects.Remove(key);
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<StorageObjectInfo> ListAsync(
            string prefix, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    // ---- Channel permission overrides (UC-CHT-H01) ----------------------------------------------

    [Fact]
    public async Task A_channel_that_turns_attaching_off_refuses_the_upload_before_any_byte_is_stored()
    {
        var space = Channel();
        var message = Text(space);
        space.SetPermissionOverrides([new ChannelPermissionEntry(ChannelPermissionSubject.Member, ChannelAction.Attach, ChannelPermissionEffect.Deny)], _owner, Now);

        var act = () => Upload(message);

        await act.Should().ThrowAsync<Notrelix.Application.Common.Exceptions.ForbiddenException>();
    }

    // ---- Rate limit (UC-CHT-H08) ------------------------------------------------------------------

    [Fact]
    public async Task An_upload_over_the_limit_is_429_before_any_byte_is_stored()
    {
        var space = Channel();
        var message = Text(space);
        _limits.Refuse(Notrelix.Application.Features.Chat.RateLimits.ChatRateLimitPolicy.Upload);

        var error = await FluentActions.Awaiting(() => Upload(message)).Should().ThrowAsync<Notrelix.Application.Common.Exceptions.TooManyRequestsException>();

        error.Which.RuleCode.Should().Be(Notrelix.Domain.Chat.Rules.ChatRuleCodes.RateLimited);
        _limits.Calls.Single().Policy.Should().Be(Notrelix.Application.Features.Chat.RateLimits.ChatRateLimitPolicy.Upload);
        _files.Files.Should().BeEmpty();
    }
}
