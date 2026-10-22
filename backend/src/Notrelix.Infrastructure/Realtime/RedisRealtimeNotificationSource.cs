using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.Infrastructure.Realtime;

/// <summary>
/// Redis pub/sub source of realtime notifications. Messages are validated and
/// parsed once per channel; malformed or foreign-schema messages are dropped and
/// logged without their content. The consumer is responsible for tenant checks
/// (see the realtime gateway), this class only maps scope to channel.
/// </summary>
public sealed class RedisRealtimeNotificationSource : IRealtimeNotificationSource
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisRealtimeNotificationSource> _logger;
    private readonly RealtimeNotificationFanout _fanout;

    public RedisRealtimeNotificationSource(
        IConnectionMultiplexer redis,
        ILogger<RedisRealtimeNotificationSource> logger)
    {
        _redis = redis;
        _logger = logger;
        _fanout = new RealtimeNotificationFanout(OpenAsync, logger);
    }

    public Task<IAsyncDisposable> SubscribeAsync(
        RealtimeScope scope,
        Func<RealtimeNotification, ValueTask> onNotification,
        CancellationToken cancellationToken)
        => _fanout.SubscribeAsync(RealtimeChannelResolver.ForScope(scope), onNotification, cancellationToken);

    private async Task<IAsyncDisposable> OpenAsync(
        string channel,
        Func<RealtimeNotification, ValueTask> dispatch,
        CancellationToken cancellationToken)
    {
        var redisChannel = RedisChannel.Literal(RealtimeChannelResolver.RedisChannel(channel));
        var subscriber = _redis.GetSubscriber();
        await subscriber.SubscribeAsync(redisChannel, (_channel, value) =>
        {
            if (!RealtimeNotificationJson.TryDeserialize(value.ToString(), out var notification))
            {
                _logger.LogWarning("Dropped malformed realtime message on channel {Channel}", channel);
                return;
            }

            // Fire-and-forget from the Redis callback: handlers enqueue and never block.
            _ = dispatch(notification);
        }).ConfigureAwait(false);

        return new Unsubscriber(subscriber, redisChannel);
    }

    private sealed class Unsubscriber(ISubscriber subscriber, RedisChannel channel) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() => await subscriber.UnsubscribeAsync(channel).ConfigureAwait(false);
    }
}

/// <summary>
/// In-process source for Development/Testing hosts and tests: no external
/// transport. <see cref="Publish"/> delivers to current subscribers of the
/// notification's stream, which makes gateway behavior testable without Redis.
/// </summary>
public sealed class InMemoryRealtimeNotificationSource : IRealtimeNotificationSource
{
    private readonly RealtimeNotificationFanout _fanout;

    public InMemoryRealtimeNotificationSource(ILogger<InMemoryRealtimeNotificationSource> logger)
    {
        _fanout = new RealtimeNotificationFanout(
            (_, _, _) => Task.FromResult<IAsyncDisposable>(new NoopTransport()), logger);
    }

    public int OpenChannelCount => _fanout.OpenChannelCount;

    public Task<IAsyncDisposable> SubscribeAsync(
        RealtimeScope scope,
        Func<RealtimeNotification, ValueTask> onNotification,
        CancellationToken cancellationToken)
        => _fanout.SubscribeAsync(RealtimeChannelResolver.ForScope(scope), onNotification, cancellationToken);

    /// <summary>Delivers a notification to every subscriber of the given channel scope.</summary>
    public ValueTask Publish(RealtimeScope scope, RealtimeNotification notification)
        => _fanout.DeliverAsync(RealtimeChannelResolver.ForScope(scope), notification);

    private sealed class NoopTransport : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
