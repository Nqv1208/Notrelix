using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Entities;

public class Attachment : BaseEntity
{
    public Guid WorkspaceId { get; private set; }
    public ResourceType ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public Guid UploadedBy { get; private set; }
    public string FileName { get; private set; } = null!;
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
        string fileName,
        string url)
    {
        return new Attachment
        {
            WorkspaceId = workspaceId,
            ResourceType = resourceType,
            ResourceId = resourceId,
            UploadedBy = uploadedBy,
            FileName = fileName,
            Url = url,
            CreatedAt = DateTime.UtcNow
        };
    }
}
