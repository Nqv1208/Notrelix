namespace Notrelix.Infrastructure.Realtime;

/// <summary>
/// Resolves realtime channel names. All resource channels are tenant-qualified
/// with workspaceId to enforce multi-tenant isolation at the channel level.
/// </summary>
public static class RealtimeChannelResolver
{
    public static string Workspace(Guid workspaceId) => $"workspace:{workspaceId}";
    public static string Board(Guid workspaceId, Guid boardId) => $"workspace:{workspaceId}:board:{boardId}";
    public static string Item(Guid workspaceId, Guid itemId) => $"workspace:{workspaceId}:item:{itemId}";
    public static string Page(Guid workspaceId, Guid pageId) => $"workspace:{workspaceId}:page:{pageId}";
    public static string UserNotifications(Guid userId) => $"user:{userId}:notifications";
}
