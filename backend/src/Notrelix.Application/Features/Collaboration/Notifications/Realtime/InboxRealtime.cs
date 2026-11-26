using System.Text.Json;
using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.Application.Features.Collaboration.Notifications.Realtime;

/// <summary>
/// The Inbox's realtime fact (UC-COL-E02): one person's Inbox changed in one Workspace, on that person's own
/// per-user stream. The frame says only which Workspace changed; the client reads the badge and the list again over
/// REST, which stays the source of truth. A frame is never delivered to anyone but its recipient (the user scope is
/// authorized for the caller's own id only).
/// </summary>
public static class InboxRealtime
{
    public const string ChangedEvent = "inbox.changed";
    public const string PayloadContract = "inbox.changed.v1";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>The payload (contract <see cref="PayloadContract"/>): <c>{ workspaceId }</c>.</summary>
    public sealed record ChangedFrame(Guid WorkspaceId);

    public static RealtimeResourceChangedV1 Changed(
        Guid? accountId, Guid workspaceId, Guid recipientUserId, Guid? correlationId, Guid? causationId, DateTimeOffset now)
    {
        var scope = new RealtimeScope(RealtimeScopeKind.User, recipientUserId);
        return new RealtimeResourceChangedV1(
            Guid.CreateVersion7(),
            accountId,
            workspaceId,
            recipientUserId,
            correlationId is { } c && c != Guid.Empty ? c : Guid.NewGuid(),
            causationId,
            now,
            scope.Topic.Namespace,
            scope.Topic.ResourceKind,
            recipientUserId,
            scope.StreamKey,
            RealtimeStreamClock.Next(now),
            ChangedEvent,
            PayloadContract,
            JsonSerializer.SerializeToElement(new ChangedFrame(workspaceId), JsonOptions));
    }
}
