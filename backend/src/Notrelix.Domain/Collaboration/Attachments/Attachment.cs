using Notrelix.Domain.Collaboration.Attachments.Events;
using Notrelix.Domain.Collaboration.Rules;
namespace Notrelix.Domain.Collaboration.Attachments;

public class Attachment : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public ResourceRef Target { get; private set; } = null!;
    public AttachmentType Type { get; private set; }
    public FileMetadata Metadata { get; private set; } = null!;

    /// <summary>
    /// True when the attachment owns an object in storage (uploaded file), false for a
    /// link attachment that only references an external URL.
    /// </summary>
    public bool IsStoredFile => Metadata.StorageKey is not null;

    /// <summary>
    /// When the stored object of this soft-deleted attachment was removed from storage after the
    /// retention period. Null while the object may still exist (and always null for links). A purged
    /// attachment cannot be restored. Rows that are not purged keep counting toward storage usage.
    /// </summary>
    public DateTimeOffset? PurgedAt { get; private set; }

    private Attachment() : base() { }

    /// <summary>
    /// Creates an attachment for a file whose bytes were already written to object storage.
    /// External facts (size cap, current attachment count and its cap) are supplied by the
    /// caller; the file rules are enforced here before any state is created, so a rejected
    /// call raises no event and creates no aggregate. The size must be the number of bytes
    /// actually stored, never a client-declared value.
    /// </summary>
    public static Attachment CreateStoredFile(
        Guid accountId,
        Guid workspaceId,
        ResourceRef target,
        AttachmentType type,
        string fileName,
        long sizeBytes,
        string contentType,
        string storageKey,
        long maxFileSizeBytes,
        int currentAttachmentCount,
        int maxAttachments,
        Guid createdBy,
        DateTimeOffset createdAt)
    {
        if (type == AttachmentType.Link)
            throw new ArgumentException("A stored file cannot be a link attachment.", nameof(type));

        Guard.NotNullOrWhiteSpace(fileName);
        Guard.NotNullOrWhiteSpace(contentType);
        Guard.NotNullOrWhiteSpace(storageKey);
        AttachmentRules.EnsureValidFileSize(sizeBytes, maxFileSizeBytes);
        AttachmentRules.EnsureMaxAttachments(currentAttachmentCount, maxAttachments);

        var metadata = FileMetadata.Create(fileName, sizeBytes, contentType, storageKey: storageKey);
        return Create(accountId, workspaceId, target, type, metadata, createdBy, createdAt);
    }

    public static Attachment Create(Guid accountId, Guid workspaceId, ResourceRef target, AttachmentType type, FileMetadata metadata, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(target);
        Guard.NotNull(metadata);

        if (target.WorkspaceId.HasValue && target.WorkspaceId.Value != workspaceId)
            throw new BusinessRuleException(CommonRuleCodes.Common_WorkspaceScopeMismatch, $"Workspace scope mismatch. Expected '{workspaceId}', got '{target.WorkspaceId.Value}'.");

        var attachment = new Attachment
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Target = target,
            Type = type,
            Metadata = metadata
        };

        attachment.SetAuditOnCreate(createdBy, createdAt);
        attachment.RaiseDomainEvent(new AttachmentCreatedDomainEvent(accountId, workspaceId, attachment.Id, target, createdAt));
        return attachment;
    }

    public void Delete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        Guard.NotEmpty(deletedBy);
        if (IsDeleted) return;
        var pendingDeletion = PrepareDeletion(deletedBy, deletedAt, reason);
        ApplyDeletion(pendingDeletion);
        IncrementVersion();
        RaiseDomainEvent(new AttachmentDeletedDomainEvent(AccountId, WorkspaceId, Id, deletedAt));
    }

    /// <summary>
    /// Records that the stored object of this soft-deleted file attachment is gone from storage.
    /// Idempotent: a repeated call keeps the first purge time. Only a deleted stored file can be
    /// purged; the retention period is a Collaboration policy decided by the caller, not here.
    /// </summary>
    public void MarkObjectPurged(DateTimeOffset purgedAt)
    {
        if (purgedAt == default)
            throw new ArgumentException("Purge time must be a valid date.", nameof(purgedAt));
        if (!IsStoredFile)
            throw new InvalidOperationException("Only a stored file attachment has an object to purge.");
        if (!IsDeleted)
            throw new InvalidOperationException("Only a deleted attachment can have its object purged.");
        if (PurgedAt is not null) return;

        PurgedAt = purgedAt;
        IncrementVersion();
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        Guard.NotEmpty(restoredBy);
        if (!IsDeleted) return;
        if (PurgedAt is not null)
            throw new BusinessRuleException(CollaborationRuleCodes.Collaboration_Attachment_ObjectPurged, "The stored file of this attachment was purged and cannot be restored.");
        var pendingRestore = PrepareRestore(restoredBy, restoredAt);
        ApplyRestore(pendingRestore);
        IncrementVersion();
        RaiseDomainEvent(new AttachmentRestoredDomainEvent(AccountId, WorkspaceId, Id, restoredBy, restoredAt));
    }
}
