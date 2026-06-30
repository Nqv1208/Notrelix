using System.Text.Json;

namespace Notrelix.Infrastructure.Data.Projections.Activity;

public sealed class WorkspaceActivityLogRecord
{
    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string SourceContext { get; private set; } = null!;
    public Guid? SourceEventId { get; private set; }
    public Guid? SourceMessageId { get; private set; }
    public string ActivityType { get; private set; } = null!;
    public Guid? ActorUserId { get; private set; }
    public string? ActorDisplayName { get; private set; }
    public string? ActorAvatarUrl { get; private set; }
    public string SubjectType { get; private set; } = null!;
    public Guid SubjectId { get; private set; }
    public string? SubjectDisplayName { get; private set; }
    public string? TargetType { get; private set; }
    public Guid? TargetId { get; private set; }
    public string? TargetDisplayName { get; private set; }
    public string? ResourceType { get; private set; }
    public Guid? ResourceId { get; private set; }
    public string? ResourceDisplayName { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Body { get; private set; }
    public JsonDocument DataJson { get; private set; } = JsonDocument.Parse("{}");
    public string Visibility { get; private set; } = "Workspace";
    public string Importance { get; private set; } = "Normal";
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    private WorkspaceActivityLogRecord() { }

    public static WorkspaceActivityLogRecord Create(
        Guid workspaceId,
        string sourceContext,
        string activityType,
        string subjectType,
        Guid subjectId,
        DateTimeOffset occurredAt,
        Guid? sourceEventId = null,
        Guid? sourceMessageId = null,
        Guid? actorUserId = null,
        string? actorDisplayName = null,
        string? actorAvatarUrl = null,
        string? subjectDisplayName = null,
        string? targetType = null,
        Guid? targetId = null,
        string? targetDisplayName = null,
        string? resourceType = null,
        Guid? resourceId = null,
        string? resourceDisplayName = null,
        string title = "",
        string? body = null,
        JsonDocument? dataJson = null,
        string visibility = "Workspace",
        string importance = "Normal")
    {
        return new WorkspaceActivityLogRecord
        {
            Id = Guid.CreateVersion7(),
            WorkspaceId = workspaceId,
            SourceContext = sourceContext,
            SourceEventId = sourceEventId,
            SourceMessageId = sourceMessageId,
            ActivityType = activityType,
            ActorUserId = actorUserId,
            ActorDisplayName = actorDisplayName,
            ActorAvatarUrl = actorAvatarUrl,
            SubjectType = subjectType,
            SubjectId = subjectId,
            SubjectDisplayName = subjectDisplayName,
            TargetType = targetType,
            TargetId = targetId,
            TargetDisplayName = targetDisplayName,
            ResourceType = resourceType,
            ResourceId = resourceId,
            ResourceDisplayName = resourceDisplayName,
            Title = title,
            Body = body,
            DataJson = dataJson ?? JsonDocument.Parse("{}"),
            Visibility = visibility,
            Importance = importance,
            OccurredAt = occurredAt,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
