using Notrelix.Domain.Chat.Messages.Events;
using Notrelix.Domain.Chat.Rules;

namespace Notrelix.Domain.Chat.Messages;

/// <summary>
/// A message in a space (CHT-005, CHT-006, CHT-007). It carries the space's sequence, assigned by the store when the
/// message is written, so a message never changes its place in the order.
/// </summary>
/// <remarks>
/// Invariants held here: content is 1 to 4,000 characters after trim (a tombstone has none); only the author edits or
/// deletes their message; a deleted message keeps its author, time and sequence and loses its content; a system
/// message cannot be edited or deleted. Who may read a space and whether it is archived is decided by the handler,
/// because it depends on the space, not on the message.
/// </remarks>
public class Message : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid SpaceId { get; private set; }
    public long Sequence { get; private set; }
    public Guid AuthorId { get; private set; }
    public MessageKind Kind { get; private set; }

    /// <summary>The text, trimmed. Null for a tombstone.</summary>
    public string? Content { get; private set; }

    /// <summary>The key the client chose for this send (unique per space and author). Null for system messages.</summary>
    public string? ClientMessageId { get; private set; }

    /// <summary>The message this one quotes in the main flow (UC-CHT-F02). Column only in this slice.</summary>
    public Guid? ReplyToId { get; private set; }

    /// <summary>The root of the thread this message belongs to (CHT-013). Null for a message of the main flow.</summary>
    public Guid? ThreadRootId { get; private set; }

    /// <summary>The reply's place in its thread: 1 for the first reply, gap-free and never reused (UC-CHT-F01). Null outside a thread.</summary>
    public long? ThreadSequence { get; private set; }

    /// <summary>A thread reply the author also sent to the channel's main flow ("also send to channel"). Always false outside a thread.</summary>
    public bool AlsoSentToChannel { get; private set; }

    public DateTimeOffset? EditedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public bool IsDeleted => DeletedAt is not null;
    public bool IsEdited => EditedAt is not null;
    public bool IsSystem => Kind == MessageKind.System;
    public bool IsThreadReply => ThreadRootId is not null;

    private Message() : base() { }

    /// <summary>A message a person sent. The sequence is the one the store assigned to the space for this send.</summary>
    public static Message CreateUserMessage(
        Guid accountId,
        Guid workspaceId,
        Guid spaceId,
        long sequence,
        Guid authorId,
        string content,
        string clientMessageId,
        Guid? replyToId,
        DateTimeOffset createdAt)
    {
        Guard.NotNullOrWhiteSpace(clientMessageId);
        if (clientMessageId.Trim().Length > ChatLimits.ClientMessageIdMaxLength)
            throw new BusinessRuleException(ChatRuleCodes.MessageContentInvalid, "The client message id is too long.");

        return Create(accountId, workspaceId, spaceId, sequence, authorId, MessageKind.User, content, createdAt, clientMessageId.Trim(), replyToId);
    }

    /// <summary>
    /// A reply in the thread of a root message (UC-CHT-F01). The space sequence orders it with the rest of the space;
    /// the thread sequence orders it inside its thread. Replies to a reply or to a system message are refused by the
    /// handler, which knows the root.
    /// </summary>
    public static Message CreateThreadReply(
        Guid accountId,
        Guid workspaceId,
        Guid spaceId,
        long sequence,
        Guid rootMessageId,
        long threadSequence,
        Guid authorId,
        string content,
        string clientMessageId,
        bool alsoSentToChannel,
        DateTimeOffset createdAt)
    {
        Guard.NotEmpty(rootMessageId);
        if (threadSequence < 1)
            throw new BusinessRuleException(ChatRuleCodes.MessageSequenceInvalid, "A thread reply sequence starts at 1.");
        Guard.NotNullOrWhiteSpace(clientMessageId);
        if (clientMessageId.Trim().Length > ChatLimits.ClientMessageIdMaxLength)
            throw new BusinessRuleException(ChatRuleCodes.MessageContentInvalid, "The client message id is too long.");

        return Create(accountId, workspaceId, spaceId, sequence, authorId, MessageKind.User, content, createdAt,
            clientMessageId.Trim(), replyToId: null, rootMessageId, threadSequence, alsoSentToChannel);
    }

    /// <summary>A system message, attributed to the person whose change it announces (UC-CHT-B09).</summary>
    public static Message CreateSystemMessage(
        Guid accountId,
        Guid workspaceId,
        Guid spaceId,
        long sequence,
        Guid actorId,
        string content,
        DateTimeOffset createdAt)
        => Create(accountId, workspaceId, spaceId, sequence, actorId, MessageKind.System, content, createdAt);

    private static Message Create(
        Guid accountId,
        Guid workspaceId,
        Guid spaceId,
        long sequence,
        Guid authorId,
        MessageKind kind,
        string content,
        DateTimeOffset createdAt,
        string? clientMessageId = null,
        Guid? replyToId = null,
        Guid? threadRootId = null,
        long? threadSequence = null,
        bool alsoSentToChannel = false)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(spaceId);
        Guard.NotEmpty(authorId);
        if (sequence < 1)
            throw new BusinessRuleException(ChatRuleCodes.MessageSequenceInvalid, "A message sequence starts at 1.");

        var normalized = NormalizeContent(content);
        var message = new Message
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            SpaceId = spaceId,
            Sequence = sequence,
            AuthorId = authorId,
            Kind = kind,
            Content = normalized,
            ClientMessageId = clientMessageId,
            ReplyToId = replyToId,
            ThreadRootId = threadRootId,
            ThreadSequence = threadSequence,
            AlsoSentToChannel = alsoSentToChannel,
        };

        message.SetAuditOnCreate(authorId, createdAt);
        message.RaiseDomainEvent(new MessageCreatedDomainEvent(
            accountId,
            workspaceId,
            message.Id,
            spaceId,
            sequence,
            authorId,
            kind,
            normalized,
            message.ClientMessageId,
            message.Version,
            createdAt,
            replyToId,
            threadRootId,
            threadSequence));

        return message;
    }

    /// <summary>Replaces the text. Only the author edits, and never a deleted or a system message. Returns false when the text is unchanged.</summary>
    public bool Edit(string content, Guid editedBy, DateTimeOffset editedAt)
    {
        EnsureEditable(editedBy);
        var normalized = NormalizeContent(content);
        if (normalized == Content) return false;

        var pending = PrepareAuditUpdate(editedBy, editedAt);
        Content = normalized;
        EditedAt = editedAt;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new MessageUpdatedDomainEvent(AccountId, WorkspaceId, Id, SpaceId, Sequence, AuthorId, normalized, Version, editedAt, ThreadRootId));
        return true;
    }

    /// <summary>
    /// Records that a file was added to this message (UC-CHT-G08): the message does not change (no version bump), the fact only
    /// lets other readers patch its file list. The caller has stored the file and checked the standing.
    /// </summary>
    public void RecordAttachmentAdded(
        Guid actorId, Guid attachmentId, string fileName, long sizeBytes, string contentType, Guid uploadedBy, DateTimeOffset createdAt, DateTimeOffset now)
    {
        Guard.NotEmpty(actorId);
        Guard.NotEmpty(attachmentId);
        RaiseDomainEvent(new MessageAttachmentChangedDomainEvent(
            AccountId, WorkspaceId, SpaceId, Id, actorId, Added: true, attachmentId, fileName, sizeBytes, contentType, uploadedBy, createdAt, now));
    }

    /// <summary>Records that a file was removed from this message (UC-CHT-G08); the message does not change.</summary>
    public void RecordAttachmentRemoved(Guid actorId, Guid attachmentId, DateTimeOffset now)
    {
        Guard.NotEmpty(actorId);
        Guard.NotEmpty(attachmentId);
        RaiseDomainEvent(new MessageAttachmentChangedDomainEvent(
            AccountId, WorkspaceId, SpaceId, Id, actorId, Added: false, attachmentId, null, null, null, null, null, now));
    }

    /// <summary>
    /// Turns the message into a tombstone: the content goes, the author, the time and the sequence stay. Only the
    /// author deletes, and a system message is never deleted by a person. Returns false when already deleted.
    /// </summary>
    public bool Delete(Guid deletedBy, DateTimeOffset deletedAt)
    {
        Guard.NotEmpty(deletedBy);
        if (IsSystem)
            throw new BusinessRuleException(ChatRuleCodes.SystemMessageImmutable, "A system message cannot be deleted.");
        if (IsDeleted) return false;
        if (deletedBy != AuthorId)
            throw new BusinessRuleException(ChatRuleCodes.MessageNotAuthor, "Only the author can delete this message.");

        var pending = PrepareAuditUpdate(deletedBy, deletedAt);
        Content = null;
        DeletedAt = deletedAt;
        DeletedBy = deletedBy;
        ApplyAuditUpdate(pending);
        IncrementVersion();
        RaiseDomainEvent(new MessageDeletedDomainEvent(AccountId, WorkspaceId, Id, SpaceId, Sequence, AuthorId, deletedBy, Version, deletedAt, ThreadRootId));
        return true;
    }

    /// <summary>
    /// Refuses a pin of a message that cannot be pinned: a system message (announcements are not content) or a tombstone
    /// (CHT-006, UC-CHT-F06). The space and membership checks belong to the handler.
    /// </summary>
    public void EnsurePinnable()
    {
        if (IsSystem)
            throw new BusinessRuleException(ChatRuleCodes.MessageNotPinnable, "A system message cannot be pinned.");
        if (IsDeleted)
            throw new BusinessRuleException(ChatRuleCodes.MessageDeleted, "A deleted message cannot be pinned.");
    }

    /// <summary>
    /// Refuses a reaction to a message that cannot carry one: a system message (announcements are not content, as with a pin) or a
    /// tombstone (UC-CHT-G06). The space and membership checks belong to the handler.
    /// </summary>
    public void EnsureReactable()
    {
        if (IsSystem)
            throw new BusinessRuleException(ChatRuleCodes.MessageNotReactable, "A system message cannot be reacted to.");
        if (IsDeleted)
            throw new BusinessRuleException(ChatRuleCodes.MessageDeleted, "A deleted message cannot be reacted to.");
    }

    /// <summary>
    /// Refuses a file on a message that cannot carry one: a system message (announcements are not content) or a tombstone
    /// (UC-CHT-G08, CHT-012). Who may attach, the space state and the file policy belong to the handler and to Collaboration.
    /// </summary>
    public void EnsureAttachable()
    {
        if (IsSystem)
            throw new BusinessRuleException(ChatRuleCodes.MessageNotAttachable, "A system message cannot carry a file.");
        if (IsDeleted)
            throw new BusinessRuleException(ChatRuleCodes.MessageDeleted, "A deleted message cannot carry a file.");
    }

    private void EnsureEditable(Guid editedBy)
    {
        Guard.NotEmpty(editedBy);
        if (IsSystem)
            throw new BusinessRuleException(ChatRuleCodes.SystemMessageImmutable, "A system message cannot be edited.");
        if (IsDeleted)
            throw new BusinessRuleException(ChatRuleCodes.MessageDeleted, "A deleted message cannot be edited.");
        if (editedBy != AuthorId)
            throw new BusinessRuleException(ChatRuleCodes.MessageNotAuthor, "Only the author can edit this message.");
    }

    private static string NormalizeContent(string? content)
    {
        Guard.NotNullOrWhiteSpace(content);
        var trimmed = content!.Trim();
        if (trimmed.Length is < 1 or > ChatLimits.MessageMaxLength)
            throw new BusinessRuleException(ChatRuleCodes.MessageContentInvalid, $"A message has 1 to {ChatLimits.MessageMaxLength} characters.");
        return trimmed;
    }
}
