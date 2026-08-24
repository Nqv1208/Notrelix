using System.Diagnostics;
using System.Text.Json;

using Notrelix.Infrastructure.Observability.Metrics;

namespace Notrelix.Infrastructure.Realtime;

/// <summary>
/// Redis-backed realtime publisher (FZ-INF-06). Publishes a JSON envelope to a
/// Redis channel per <see cref="RealtimeChannelResolver"/>. The envelope follows
/// the frontend realtime contract (eventId/eventType/workspaceId/correlationId/
/// timestamp), so a future WebSocket bridge can forward it without re-mapping.
///
/// This is the production implementation of <see cref="IRealtimePublisher"/>.
/// It runs in the broker-consumer realtime path after durable commit; failures
/// are logged and retried by the outbox/inbox delivery, never thrown into the
/// committed request result.
/// </summary>
public sealed class RedisRealtimePublisher : IRealtimePublisher
{
    private const string ChannelPrefix = "notrelix:realtime:";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

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
        var topic = new RealtimeTopic(change.TopicNamespace, change.ResourceKind, change.ResourceId);
        var channel = ResolveChannel(topic);
        var envelope = new RealtimeEnvelopeDto(
            EventId: change.EventId.ToString("N"),
            EventType: $"{topic.Namespace}.{topic.ResourceKind}".ToLowerInvariant(),
            WorkspaceId: change.WorkspaceId?.ToString() ?? string.Empty,
            CorrelationId: change.CorrelationId.ToString("N"),
            Timestamp: change.OccurredAt.ToString("O"),
            SchemaVersion: 1,
            ResourceId: change.ResourceId,
            StreamVersion: change.StreamVersion,
            PayloadContract: change.PayloadContract,
            Payload: change.Payload);

        var json = JsonSerializer.Serialize(envelope, JsonOptions);

        _logger.LogTrace(
            "Publishing realtime event {EventType} to channel {Channel}",
            envelope.EventType, ChannelPrefix + channel);

        var stopwatch = Stopwatch.StartNew();
        await _redis.GetDatabase().PublishAsync(RedisChannel.Literal(ChannelPrefix + channel), json);
        stopwatch.Stop();

        _metrics.RealtimePublishDuration.Record(stopwatch.Elapsed.TotalMilliseconds);
    }

    /// <summary>
    /// Channel resolution: workspace-namespace topics map to the tenant-qualified
    /// workspace channel; all other topics map to a resource-qualified channel.
    /// </summary>
    internal static string ResolveChannel(RealtimeTopic topic)
        => topic.Namespace == "workspace"
            ? RealtimeChannelResolver.Workspace(topic.ResourceId)
            : $"resource:{topic.Namespace}:{topic.ResourceKind}:{topic.ResourceId}";

    private sealed record RealtimeEnvelopeDto(
        string EventId,
        string EventType,
        string WorkspaceId,
        string CorrelationId,
        string Timestamp,
        int SchemaVersion,
        Guid ResourceId,
        long StreamVersion,
        string PayloadContract,
        object Payload);
}
