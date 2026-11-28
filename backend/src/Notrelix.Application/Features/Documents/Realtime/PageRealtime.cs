using System.Text.Json;
using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.Application.Features.Documents.Realtime;

/// <summary>
/// The Page stream's realtime fact (UC-COL-E01, UC-DCT-K01): something a reader of one page sees changed (the page
/// itself, one of its blocks, or a comment on it). The frame is notify-only, like the Board stream: the client reads
/// the blocks, the page and its comments again over REST, where ViewPage is checked again.
/// </summary>
public static class PageRealtime
{
    public const string ChangedEvent = "page.changed";
    public const string PayloadContract = "page.changed.v1";

    private static readonly JsonElement EmptyPayload = JsonSerializer.SerializeToElement(new { });

    public static RealtimeResourceChangedV1 Changed(
        Guid accountId, Guid workspaceId, Guid pageId, Guid? actorUserId, Guid? correlationId, Guid? causationId, DateTimeOffset now)
    {
        var scope = new RealtimeScope(RealtimeScopeKind.Page, pageId);
        return new RealtimeResourceChangedV1(
            Guid.CreateVersion7(),
            accountId,
            workspaceId,
            actorUserId,
            correlationId is { } c && c != Guid.Empty ? c : Guid.NewGuid(),
            causationId,
            now,
            scope.Topic.Namespace,
            scope.Topic.ResourceKind,
            pageId,
            scope.StreamKey,
            RealtimeStreamClock.Next(now),
            ChangedEvent,
            PayloadContract,
            EmptyPayload);
    }
}
