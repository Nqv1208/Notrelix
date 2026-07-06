using System.Text.Json;

namespace Notrelix.Infrastructure.Data.Analytics;

public sealed class WorkspaceUsageDaily
{
    public Guid WorkspaceId { get; private set; }
    public DateOnly UsageDate { get; private set; }
    public int ActiveUsers { get; private set; }
    public int NewUsers { get; private set; }
    public int BoardsCreated { get; private set; }
    public int ItemsCreated { get; private set; }
    public int ItemsCompleted { get; private set; }
    public int DocsCreated { get; private set; }
    public int CommentsCreated { get; private set; }
    public int AutomationsExecuted { get; private set; }
    public int IntegrationsExecuted { get; private set; }
    public long StorageBytes { get; private set; }
    public int AttachmentCount { get; private set; }
    public JsonDocument MetadataJson { get; private set; } = JsonDocument.Parse("{}");
    public DateTimeOffset CalculatedAt { get; private set; }
    public DateTimeOffset? SourceWatermarkAt { get; private set; }

    private WorkspaceUsageDaily() { }

    public WorkspaceUsageDaily(
        Guid workspaceId,
        DateOnly usageDate,
        int activeUsers,
        int newUsers,
        int boardsCreated,
        int itemsCreated,
        int itemsCompleted,
        int docsCreated,
        int commentsCreated,
        int automationsExecuted,
        int integrationsExecuted,
        long storageBytes,
        int attachmentCount,
        JsonDocument? metadataJson,
        DateTimeOffset calculatedAt)
    {
        WorkspaceId = workspaceId;
        UsageDate = usageDate;
        ActiveUsers = activeUsers;
        NewUsers = newUsers;
        BoardsCreated = boardsCreated;
        ItemsCreated = itemsCreated;
        ItemsCompleted = itemsCompleted;
        DocsCreated = docsCreated;
        CommentsCreated = commentsCreated;
        AutomationsExecuted = automationsExecuted;
        IntegrationsExecuted = integrationsExecuted;
        StorageBytes = storageBytes;
        AttachmentCount = attachmentCount;
        MetadataJson = metadataJson ?? JsonDocument.Parse("{}");
        CalculatedAt = calculatedAt;
    }
}
