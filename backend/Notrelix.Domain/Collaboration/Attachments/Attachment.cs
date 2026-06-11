using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Attachments;

public class Attachment : SoftDeletableEntity
{
    public Guid WorkspaceId { get; private set; }
    public ResourceRef Target { get; private set; } = null!;
    public AttachmentType Type { get; private set; }
    public FileMetadata Metadata { get; private set; } = null!;

    private Attachment() : base() { }

    public static Attachment Create(Guid workspaceId, ResourceRef target, AttachmentType type, FileMetadata metadata, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNull(target);
        Guard.NotNull(metadata);

        var attachment = new Attachment
        {
            WorkspaceId = workspaceId,
            Target = target,
            Type = type,
            Metadata = metadata
        };

        attachment.SetAuditOnCreate(createdBy, createdAt);
        attachment.AddDomainEvent(new AttachmentCreatedEvent(workspaceId, attachment.Id, target, createdAt));
        return attachment;
    }

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        AddDomainEvent(new AttachmentDeletedEvent(WorkspaceId, Id, deletedAt));
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
    }
}
