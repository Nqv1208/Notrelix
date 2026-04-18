using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? ActorId { get; private set; }
    public string Type { get; private set; } = null!;
    public string Payload { get; private set; } = "{}";
    public ResourceType? ResourceType { get; private set; }
    public Guid? ResourceId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Notification() : base() { }

    public static Notification Create(Guid workspaceId, Guid userId, string type, string payload = "{}")
    {
        return new Notification
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            Type = type,
            Payload = string.IsNullOrWhiteSpace(payload) ? "{}" : payload,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsRead()
    {
        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }
}
