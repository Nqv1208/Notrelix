namespace Notrelix.Infrastructure.Realtime;

/// <summary>
/// Resolves realtime channel names (v4 §11). Pure/inert string helper shared by
/// publishers/hubs so channel naming stays consistent.
/// </summary>
public static class RealtimeChannelResolver
{
    public static string Workspace(Guid workspaceId) => $"workspace:{workspaceId}";
    public static string Board(Guid boardId) => $"board:{boardId}";
    public static string Item(Guid itemId) => $"item:{itemId}";
    public static string Page(Guid pageId) => $"page:{pageId}";
    public static string UserNotifications(Guid userId) => $"user:{userId}:notifications";
}
