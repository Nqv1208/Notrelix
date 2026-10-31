using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.Infrastructure.Realtime;

/// <summary>
/// Redis pub/sub source of realtime notifications. Messages are validated and
/// parsed once per channel; malformed or foreign-schema messages are dropped and
/// logged without their content. The consumer is responsible for tenant checks
/// (see the realtime gateway), this class only maps scope to channel.
/// </summary>
public sealed class RedisRealtimeNotificationSource : IRealtimeNotificationSource, IRealtimeAccessSignalSource
{
    private readonly ILogger<RedisRealtimeNotificationSource> _logger;
    private readonly Func<string, Action<string>, Task<IAsyncDisposable>> _openChannel;
    private readonly RealtimeNotificationFanout _fanout;

    public RedisRealtimeNotificationSource(
        IConnectionMultiplexer redis,
        ILogger<RedisRealtimeNotificationSource> logger)
        : this(logger, (channel, onMessage) => OpenQueueAsync(redis, channel, onMessage))
    {
    }

    /// <summary>
    /// Seam for tests: <paramref name="openChannel"/> opens the transport channel and calls
    /// the given callback once per message, one at a time and in arrival order.
    /// </summary>
    public RedisRealtimeNotificationSource(
        ILogger<RedisRealtimeNotificationSource> logger,
        Func<string, Action<string>, Task<IAsyncDisposable>> openChannel)
    {
        _logger = logger;
        _openChannel = openChannel;
        _fanout = new RealtimeNotificationFanout(OpenAsync, logger);
    }

    public Task<IAsyncDisposable> SubscribeAsync(
        RealtimeScope scope,
        Func<RealtimeNotification, ValueTask> onNotification,
        CancellationToken cancellationToken)
        => _fanout.SubscribeAsync(RealtimeChannelResolver.ForScope(scope), onNotification, cancellationToken);

    public Task<IAsyncDisposable> SubscribeAccessAsync(
        Guid workspaceId,
        Func<RealtimeNotification, ValueTask> onSignal,
        CancellationToken cancellationToken)
        => _fanout.SubscribeAsync(RealtimeChannelResolver.Access(workspaceId), onSignal, cancellationToken);

    private Task<IAsyncDisposable> OpenAsync(
        string channel,
        Func<RealtimeNotification, ValueTask> dispatch,
        CancellationToken cancellationToken)
        => _openChannel(RealtimeChannelResolver.RedisChannel(channel), json =>
        {
            if (!RealtimeNotificationJson.TryDeserialize(json, out var notification))
            {
                _logger.LogWarning("Dropped malformed realtime message on channel {Channel}", channel);
                return;
            }

            // Handlers enqueue and never block the transport callback.
            _ = dispatch(notification);
        });

    /// <summary>
    /// Opens a Redis channel as a <see cref="ChannelMessageQueue"/>. The handler overload of
    /// Subscribe runs callbacks concurrently on the thread pool, which reorders the versions of
    /// one stream; the queue hands messages to <c>OnMessage</c> one at a time in arrival order,
    /// which the client's per-stream version check relies on.
    /// </summary>
    private static async Task<IAsyncDisposable> OpenQueueAsync(
        IConnectionMultiplexer redis,
        string channel,
        Action<string> onMessage)
    {
        var queue = await redis.GetSubscriber()
            .SubscribeAsync(RedisChannel.Literal(channel)).ConfigureAwait(false);
        queue.OnMessage(message => onMessage(message.Message.ToString()));
        return new QueueSubscription(queue);
    }

    private sealed class QueueSubscription(ChannelMessageQueue queue) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() => await queue.UnsubscribeAsync().ConfigureAwait(false);
    }
}

/// <summary>
/// In-process source for Development/Testing hosts and tests: no external
/// transport. <see cref="Publish"/> delivers to current subscribers of the
/// notification's stream, which makes gateway behavior testable without Redis.
/// </summary>
public sealed class InMemoryRealtimeNotificationSource : IRealtimeNotificationSource, IRealtimeAccessSignalSource
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

    public Task<IAsyncDisposable> SubscribeAccessAsync(
        Guid workspaceId,
        Func<RealtimeNotification, ValueTask> onSignal,
        CancellationToken cancellationToken)
        => _fanout.SubscribeAsync(RealtimeChannelResolver.Access(workspaceId), onSignal, cancellationToken);

    /// <summary>Delivers an access signal to every connection of the workspace on this instance.</summary>
    public ValueTask PublishAccess(Guid workspaceId, RealtimeNotification signal)
        => _fanout.DeliverAsync(RealtimeChannelResolver.Access(workspaceId), signal);

    /// <summary>Delivers a notification to every subscriber of the given channel scope.</summary>
    public ValueTask Publish(RealtimeScope scope, RealtimeNotification notification)
        => _fanout.DeliverAsync(RealtimeChannelResolver.ForScope(scope), notification);

    private sealed class NoopTransport : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
