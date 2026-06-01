using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Entities.Shared;

// Activity log — append-only, partitioned by month
public class ActivityLog
{
    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid ActorId { get; private set; }
    public string Action { get; private set; } = null!;
    public ResourceType ResourceType { get; private set; }
    public Guid ResourceId { get; private set; }
    public string? ResourceTitle { get; private set; }
    public string Metadata { get; private set; } = "{}";
    public string? IpAddress { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ActivityLog() { }

    public static ActivityLog Create(
        Guid workspaceId,
        Guid actorId,
        string action,
        ResourceType resourceType,
        Guid resourceId,
        string? resourceTitle = null,
        string metadata = "{}",
        string? ipAddress = null)
    {
        return new ActivityLog
        {
            Id = Guid.CreateVersion7(),
            WorkspaceId = workspaceId,
            ActorId = actorId,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            ResourceTitle = resourceTitle,
            Metadata = string.IsNullOrWhiteSpace(metadata) ? "{}" : metadata,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };
    }
}
