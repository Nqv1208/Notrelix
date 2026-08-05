using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Notrelix.Application.Common.PostCommit;

namespace Notrelix.Infrastructure.Realtime;

/// <summary>
/// Redis-backed realtime publisher (FZ-INF-06). Publishes a JSON envelope to a
/// Redis channel per <see cref="RealtimeChannelResolver"/>. The envelope follows
/// the frontend realtime contract (eventId/eventType/workspaceId/correlationId/
/// timestamp), so a future WebSocket bridge can forward it without re-mapping.
///
/// This is the production implementation of <see cref="IRealtimePublisher"/>.
/// Failures are handled by the post-commit action queue (logged, never thrown),
/// so a Redis outage cannot corrupt the committed request result (RULE.md §35).
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

    public RedisRealtimePublisher(
        IConnectionMultiplexer redis,
        ILogger<RedisRealtimePublisher> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task PublishAsync(RealtimeTopic topic, object payload, CancellationToken cancellationToken)
    {
        var channel = ResolveChannel(topic);
        var envelope = new RealtimeEnvelopeDto(
            EventId: Guid.NewGuid().ToString("N"),
            EventType: $"{topic.Namespace}.{topic.ResourceKind}".ToLowerInvariant(),
            WorkspaceId: topic.Namespace == "workspace" ? topic.ResourceId.ToString() : string.Empty,
            CorrelationId: string.Empty,
            Timestamp: DateTimeOffset.UtcNow.ToString("O"),
            SchemaVersion: 1,
            Payload: payload);

        var json = JsonSerializer.Serialize(envelope, JsonOptions);

        _logger.LogTrace(
            "Publishing realtime event {EventType} to channel {Channel}",
            envelope.EventType, ChannelPrefix + channel);

        await _redis.GetDatabase().PublishAsync(RedisChannel.Literal(ChannelPrefix + channel), json);
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
        object Payload);
}
