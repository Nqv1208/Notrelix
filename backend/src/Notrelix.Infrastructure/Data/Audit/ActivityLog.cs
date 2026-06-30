using System.Text.Json;

namespace Notrelix.Infrastructure.Data.Audit;

public sealed class ActivityLog
{
    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string? ActorDisplayName { get; private set; }
    public string ActivityType { get; private set; } = null!;
    public string? Verb { get; private set; }
    public string? ResourceType { get; private set; }
    public Guid? ResourceId { get; private set; }
    public string? ResourceName { get; private set; }
    public string? TargetResourceType { get; private set; }
    public Guid? TargetResourceId { get; private set; }
    public string? TargetResourceName { get; private set; }
    public string? Summary { get; private set; }
    public JsonDocument MetadataJson { get; private set; } = JsonDocument.Parse("{}");
    public string? CorrelationId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
    public bool IsVisible { get; private set; } = true;
    public DateTimeOffset? HiddenAt { get; private set; }
    public Guid? HiddenBy { get; private set; }
    public string? HideReason { get; private set; }

    private ActivityLog() { }

    public ActivityLog(
        Guid workspaceId,
        Guid? actorUserId,
        string? actorDisplayName,
        string activityType,
        string? verb,
        string? resourceType,
        Guid? resourceId,
        string? resourceName,
        string? targetResourceType,
        Guid? targetResourceId,
        string? targetResourceName,
        string? summary,
        JsonDocument? metadataJson,
        string? correlationId,
        DateTimeOffset occurredAt)
    {
        Id = Guid.CreateVersion7();
        WorkspaceId = workspaceId;
        ActorUserId = actorUserId;
        ActorDisplayName = actorDisplayName;
        ActivityType = activityType;
        Verb = verb;
        ResourceType = resourceType;
        ResourceId = resourceId;
        ResourceName = resourceName;
        TargetResourceType = targetResourceType;
        TargetResourceId = targetResourceId;
        TargetResourceName = targetResourceName;
        Summary = summary;
        MetadataJson = metadataJson ?? JsonDocument.Parse("{}");
        CorrelationId = correlationId;
        OccurredAt = occurredAt;
        RecordedAt = occurredAt;
    }
}
