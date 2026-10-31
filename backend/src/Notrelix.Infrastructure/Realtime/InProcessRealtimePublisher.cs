using System.Collections.Concurrent;
using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.Infrastructure.Realtime;

/// <summary>
/// Development publisher: delivers a notify-only <see cref="RealtimeNotification"/> to the sockets of this
/// process, so a second browser converges without Redis. Same notification shape as the Redis publisher,
/// with the per-stream version counted in memory. Runs after the outbox committed, like the Redis one.
/// shortcut: one API process only, no Redis; set Realtime:Transport=Redis for several instances.
/// </summary>
public sealed class InProcessRealtimePublisher : IRealtimePublisher
{
    private static readonly ConcurrentDictionary<string, long> Versions = new();

    private readonly InMemoryRealtimeNotificationSource _source;

    public InProcessRealtimePublisher(InMemoryRealtimeNotificationSource source) => _source = source;

    public async Task PublishAsync(RealtimeResourceChangedV1 change, CancellationToken cancellationToken)
    {
        if (change.WorkspaceId is not { } workspaceId || workspaceId == Guid.Empty) return;

        if (RealtimeAccessSignalMapping.IsAccessSignal(change))
        {
            await _source.PublishAccess(workspaceId, RealtimeAccessSignalMapping.ToNotification(change, workspaceId));
            return;
        }

        var kind = change.TopicNamespace switch
        {
            "workspace" => RealtimeScopeKind.Workspace,
            "board" => RealtimeScopeKind.Board,
            _ => (RealtimeScopeKind?)null,
        };
        if (kind is null) return;

        var scope = new RealtimeScope(kind.Value, change.ResourceId);
        var version = Versions.AddOrUpdate(change.StreamKey, 1, (_, last) => last + 1);
        await _source.Publish(scope, new RealtimeNotification(
            EventId: change.EventId.ToString("N"),
            EventType: $"{change.TopicNamespace}.{change.ResourceKind}".ToLowerInvariant(),
            ChangeKind: change.ChangeKind,
            StreamKey: change.StreamKey,
            StreamVersion: version,
            ResourceKind: change.ResourceKind,
            ResourceId: change.ResourceId,
            WorkspaceId: workspaceId,
            CorrelationId: change.CorrelationId.ToString("N"),
            OccurredAt: change.OccurredAt,
            ResourceVersion: change.StreamVersion,
            SchemaVersion: 1,
            PayloadContract: change.PayloadContract));
    }
}
