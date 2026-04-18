using Notrelix.Domain.Common;
using Notrelix.Domain.Enums;

namespace Notrelix.Domain.Entities;

// Entity ghi lại tất cả hoạt động trong hệ thống (Audit Log)
public class ActivityLog : BaseEntity
{
    public Guid ActorId { get; private set; }
    public ActivityAction Action { get; private set; }
    public string TargetType { get; private set; } = null!; // Entity type name
    public Guid TargetId { get; private set; }
    public string? TargetTitle { get; private set; }
    public Guid? WorkspaceId { get; private set; }
    public string Metadata { get; private set; } = "{}"; // JSON - additional info
    public DateTime CreatedAt { get; private set; }

    private ActivityLog() : base() { }

    public static ActivityLog Create(
        Guid actorId,
        ActivityAction action,
        string targetType,
        Guid targetId,
        string? targetTitle = null,
        Guid? workspaceId = null,
        object? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(targetType))
            throw new ArgumentException("Target type không được để trống", nameof(targetType));

        return new ActivityLog
        {
            ActorId = actorId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            TargetTitle = targetTitle,
            WorkspaceId = workspaceId,
            Metadata = metadata != null 
                ? System.Text.Json.JsonSerializer.Serialize(metadata) 
                : "{}",
            CreatedAt = DateTime.UtcNow
        };
    }

    // Factory methods for common activities
    public static ActivityLog ContentCreated(Guid actorId, string contentType, Guid contentId, string title, Guid workspaceId)
    {
        return Create(actorId, ActivityAction.Create, contentType, contentId, title, workspaceId);
    }

    public static ActivityLog ContentUpdated(Guid actorId, string contentType, Guid contentId, string title, Guid workspaceId, object? changes = null)
    {
        return Create(actorId, ActivityAction.Update, contentType, contentId, title, workspaceId, changes);
    }

    public static ActivityLog ContentDeleted(Guid actorId, string contentType, Guid contentId, string title, Guid workspaceId)
    {
        return Create(actorId, ActivityAction.Delete, contentType, contentId, title, workspaceId);
    }

    public static ActivityLog TodoCompleted(Guid actorId, Guid todoId, string title, Guid workspaceId)
    {
        return Create(actorId, ActivityAction.Complete, "Todo", todoId, title, workspaceId);
    }

    public static ActivityLog MemberJoined(Guid actorId, Guid workspaceId, string workspaceName)
    {
        return Create(actorId, ActivityAction.Join, "Workspace", workspaceId, workspaceName, workspaceId);
    }

    public static ActivityLog CommentAdded(Guid actorId, string resourceType, Guid resourceId, string? resourceTitle, Guid workspaceId, Guid commentId)
    {
        return Create(actorId, ActivityAction.Comment, resourceType, resourceId, resourceTitle, workspaceId, new { CommentId = commentId });
    }

    // Get metadata as typed object
    public T? GetMetadata<T>() where T : class
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(Metadata);
        }
        catch
        {
            return null;
        }
    }
}
