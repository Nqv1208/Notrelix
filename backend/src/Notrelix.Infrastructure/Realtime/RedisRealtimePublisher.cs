using System.Diagnostics;

using Notrelix.Application.Common.Realtime.Gateway;
using Notrelix.Infrastructure.Observability.Metrics;

namespace Notrelix.Infrastructure.Realtime;

/// <summary>
/// Redis-backed realtime publisher (FZ-INF-06). Publishes a notify-only
/// <see cref="RealtimeNotification"/> to the channel resolved by
/// <see cref="RealtimeChannelResolver"/>. The resource payload is deliberately not
/// published: realtime only notifies, clients refetch authoritative state over REST.
///
/// The per-stream <c>streamVersion</c> is allocated by a Redis counter in the same
/// atomic script that publishes the message, so for one stream the messages are
/// published in strictly consecutive version order. A missing version at a
/// subscriber therefore means a lost message, which is what gap recovery detects.
///
/// This is the production implementation of <see cref="IRealtimePublisher"/>.
/// It runs in the broker-consumer realtime path after durable commit; failures
/// are logged and retried by the outbox/inbox delivery, never thrown into the
/// committed request result. A retry after an ambiguous failure may publish the
/// same event again with the next stream version; notifications are idempotent
/// (clients dedupe by event id and refetch).
/// </summary>
public sealed class RedisRealtimePublisher : IRealtimePublisher
{
    /// <summary>Counter retention; an expired counter restarts at 1 and clients refetch.</summary>
    private static readonly long CounterRetentionMilliseconds = (long)TimeSpan.FromDays(30).TotalMilliseconds;

    // KEYS[1] = stream counter key, KEYS[2] = channel
    // ARGV[1] = message prefix up to the stream version, ARGV[2] = suffix, ARGV[3] = counter ttl (ms)
    private const string PublishScript = """
        local version = redis.call('INCR', KEYS[1])
        redis.call('PEXPIRE', KEYS[1], ARGV[3])
        redis.call('PUBLISH', KEYS[2], ARGV[1] .. version .. ARGV[2])
        return version
        """;

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisRealtimePublisher> _logger;
    private readonly MetricsService _metrics;

    public RedisRealtimePublisher(
        IConnectionMultiplexer redis,
        ILogger<RedisRealtimePublisher> logger,
        MetricsService metrics)
    {
        _redis = redis;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task PublishAsync(RealtimeResourceChangedV1 change, CancellationToken cancellationToken)
    {
        if (change.WorkspaceId is not { } workspaceId || workspaceId == Guid.Empty)
        {
            // A notification without a tenant binding can never be delivered to an
            // authorized subscriber; do not publish tenant-less realtime facts.
            _logger.LogWarning(
                "Skipping realtime event {EventType} for stream {StreamKey}: no workspace binding",
                change.MessageName, change.StreamKey);
            return;
        }

        if (RealtimeAccessSignalMapping.IsAccessSignal(change))
        {
            // A signal needs no per-stream counter: a lost one is covered by the periodic re-authorization.
            await _redis.GetSubscriber().PublishAsync(
                RedisChannel.Literal(RealtimeChannelResolver.RedisChannel(RealtimeChannelResolver.Access(workspaceId))),
                RealtimeNotificationJson.Serialize(RealtimeAccessSignalMapping.ToNotification(change, workspaceId)));
            return;
        }

        var topic = new RealtimeTopic(change.TopicNamespace, change.ResourceKind, change.ResourceId);
        var channel = RealtimeChannelResolver.RedisChannel(RealtimeChannelResolver.ForTopic(topic));
        var draft = new RealtimeNotification(
            EventId: change.EventId.ToString("N"),
            EventType: $"{topic.Namespace}.{topic.ResourceKind}".ToLowerInvariant(),
            ChangeKind: change.ChangeKind,
            StreamKey: change.StreamKey,
            StreamVersion: 0,
            ResourceKind: topic.ResourceKind,
            ResourceId: change.ResourceId,
            WorkspaceId: workspaceId,
            CorrelationId: change.CorrelationId.ToString("N"),
            OccurredAt: change.OccurredAt,
            ResourceVersion: change.StreamVersion,
            SchemaVersion: 1,
            PayloadContract: change.PayloadContract);

        var (prefix, suffix) = RealtimeNotificationJson.SerializeForVersionSplice(draft);

        _logger.LogTrace(
            "Publishing realtime event {EventType} to channel {Channel}",
            draft.EventType, channel);

        var stopwatch = Stopwatch.StartNew();
        await _redis.GetDatabase().ScriptEvaluateAsync(
            PublishScript,
            [RealtimeChannelResolver.StreamCounterKey(change.StreamKey), channel],
            [prefix, suffix, CounterRetentionMilliseconds]);
        stopwatch.Stop();

        _metrics.RealtimePublishDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
    }
}
