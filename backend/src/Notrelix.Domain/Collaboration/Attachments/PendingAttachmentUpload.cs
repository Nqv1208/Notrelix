using Notrelix.Domain.Collaboration.Rules;

namespace Notrelix.Domain.Collaboration.Attachments;

/// <summary>
/// A file that was declared for a target and is waiting for its bytes in object storage (the pre-signed two-step upload). It owns the
/// server-generated storage key, the declared facts, the voice hints and an expiry. It is not an attachment: no attachment row exists until
/// the commit verifies the stored bytes, so an expired or abandoned upload never shows up as a file. Once committed it records the id of
/// the attachment it produced, which makes a repeated commit a replay instead of a second file.
/// </summary>
public class PendingAttachmentUpload : AggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public ResourceRef Target { get; private set; } = null!;
    public string StorageKey { get; private set; } = null!;
    public string FileName { get; private set; } = null!;
    public long DeclaredSize { get; private set; }

    /// <summary>The largest byte count the commit accepts: the file cap, lowered to the voice ceiling for a voice message.</summary>
    public long CeilingBytes { get; private set; }
    public string DeclaredContentType { get; private set; } = null!;
    public bool Voice { get; private set; }
    public int? DurationHintMs { get; private set; }
    public byte[]? Waveform { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public Guid? AttachmentId { get; private set; }
    public DateTimeOffset? CommittedAt { get; private set; }

    private PendingAttachmentUpload() : base() { }

    public bool IsCommitted => AttachmentId is not null;

    public bool IsExpiredAt(DateTimeOffset now) => now >= ExpiresAt;

    public static PendingAttachmentUpload Create(
        Guid accountId,
        Guid workspaceId,
        ResourceRef target,
        string storageKey,
        string fileName,
        long declaredSize,
        string declaredContentType,
        long ceilingBytes,
        bool voice,
        int? durationHintMs,
        byte[]? waveform,
        Guid createdBy,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(target);
        Guard.NotNullOrWhiteSpace(storageKey);
        Guard.NotNullOrWhiteSpace(fileName);
        Guard.NotNullOrWhiteSpace(declaredContentType);
        AttachmentRules.EnsureValidFileSize(declaredSize, ceilingBytes);
        if (expiresAt <= createdAt)
            throw new ArgumentException("An upload must expire after it was created.", nameof(expiresAt));

        var pending = new PendingAttachmentUpload
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Target = target,
            StorageKey = storageKey,
            FileName = fileName,
            DeclaredSize = declaredSize,
            CeilingBytes = ceilingBytes,
            DeclaredContentType = declaredContentType,
            Voice = voice,
            DurationHintMs = durationHintMs,
            Waveform = waveform,
            ExpiresAt = expiresAt,
        };
        pending.SetAuditOnCreate(createdBy, createdAt);
        return pending;
    }

    /// <summary>Records the attachment the verified bytes became. A second call is refused: a committed upload cannot produce another file.</summary>
    public void MarkCommitted(Guid attachmentId, DateTimeOffset committedAt)
    {
        Guard.NotEmpty(attachmentId);
        if (IsCommitted)
            throw new BusinessRuleException(CollaborationRuleCodes.Collaboration_Attachment_UploadAlreadyCommitted, "This upload was already committed.");

        AttachmentId = attachmentId;
        CommittedAt = committedAt;
        IncrementVersion();
    }
}
