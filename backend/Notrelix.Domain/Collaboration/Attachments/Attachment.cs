using Notrelix.Domain.Common;

namespace Notrelix.Domain.Collaboration.Attachments;

public class Attachment : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public ResourceRef Target { get; private set; } = null!;
    public AttachmentType Type { get; private set; }
    public FileMetadata Metadata { get; private set; } = null!;

    private Attachment() : base() { }

    public static Attachment Create(Guid workspaceId, ResourceRef target, AttachmentType type, FileMetadata metadata, Guid createdBy)
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

        attachment.SetAuditOnCreate(createdBy);
        return attachment;
    }
}
