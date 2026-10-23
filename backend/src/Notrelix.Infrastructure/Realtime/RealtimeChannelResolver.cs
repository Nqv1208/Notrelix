using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.Infrastructure.Realtime;

/// <summary>
/// Resolves realtime channel names. Publisher and subscriber resolve through the
/// same functions so a scope can only ever receive the stream it was authorized
/// for. Tenant isolation does not rely on channel names alone: subscriptions are
/// authorized per scope and every delivered notification is re-checked against
/// the authorized workspace.
/// </summary>
public static class RealtimeChannelResolver
{
    public const string RedisChannelPrefix = "notrelix:realtime:";

    public static string Workspace(Guid workspaceId) => $"workspace:{workspaceId}";
    public static string Board(Guid workspaceId, Guid boardId) => $"workspace:{workspaceId}:board:{boardId}";
    public static string Item(Guid workspaceId, Guid itemId) => $"workspace:{workspaceId}:item:{itemId}";
    public static string Page(Guid workspaceId, Guid pageId) => $"workspace:{workspaceId}:page:{pageId}";
    public static string UserNotifications(Guid userId) => $"user:{userId}:notifications";

    /// <summary>
    /// Workspace-namespace topics map to the workspace channel; all other topics
    /// map to a resource-qualified channel.
    /// </summary>
    public static string ForTopic(RealtimeTopic topic)
        => topic.Namespace == "workspace"
            ? Workspace(topic.ResourceId)
            : $"resource:{topic.Namespace}:{topic.ResourceKind}:{topic.ResourceId}";

    public static string ForScope(RealtimeScope scope) => ForTopic(scope.Topic);

    public static string RedisChannel(string channel) => RedisChannelPrefix + channel;

    /// <summary>Redis key of the per-stream delivery counter.</summary>
    public static string StreamCounterKey(string streamKey) => RedisChannelPrefix + "seq:" + streamKey;
}
