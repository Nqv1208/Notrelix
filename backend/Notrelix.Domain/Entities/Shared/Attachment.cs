using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Entities.Shared;

// Entity đại diện cho file đính kèm
public class Attachment : BaseEntity
{
    public Guid WorkspaceId { get; private set; }
    public ResourceType ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public Guid UploadedBy { get; private set; }
    public string Filename { get; private set; } = null!;
    public string Url { get; private set; } = null!;
    public long? SizeBytes { get; private set; }
    public string? MimeType { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Attachment() : base() { }

    public static Attachment Create(
        Guid workspaceId,
        ResourceType resourceType,
        Guid resourceId,
        Guid uploadedBy,
        string filename,
        string url,
        long? sizeBytes = null,
        string? mimeType = null)
    {
        return new Attachment
        {
            WorkspaceId = workspaceId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            UploadedBy = uploadedBy,
            Filename = filename.Trim(),
            Url = url.Trim(),
            SizeBytes = sizeBytes,
            MimeType = mimeType,
            CreatedAt = DateTime.UtcNow
        };
    }
}
