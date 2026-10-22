using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.Infrastructure.Realtime;

/// <summary>
/// Ref-counted fan-out of one transport channel to many handlers. The transport
/// subscription is opened for the first handler and closed with the last, so a
/// process holds at most one transport subscription per channel regardless of
/// connection count. Handler failures are isolated from each other.
/// </summary>
internal sealed class RealtimeNotificationFanout
{
    private readonly object _gate = new();
    private readonly Dictionary<string, ChannelEntry> _channels = new(StringComparer.Ordinal);
    private readonly Func<string, Func<RealtimeNotification, ValueTask>, CancellationToken, Task<IAsyncDisposable>> _openTransport;
    private readonly ILogger _logger;

    public RealtimeNotificationFanout(
        Func<string, Func<RealtimeNotification, ValueTask>, CancellationToken, Task<IAsyncDisposable>> openTransport,
        ILogger logger)
    {
        _openTransport = openTransport;
        _logger = logger;
    }

    public int OpenChannelCount
    {
        get
        {
            lock (_gate)
            {
                return _channels.Count;
            }
        }
    }

    public async Task<IAsyncDisposable> SubscribeAsync(
        string channel,
        Func<RealtimeNotification, ValueTask> handler,
        CancellationToken cancellationToken)
    {
        ChannelEntry entry;
        lock (_gate)
        {
            if (!_channels.TryGetValue(channel, out entry!))
            {
                entry = new ChannelEntry(channel);
                _channels[channel] = entry;
            }

            entry.Handlers.Add(handler);
        }

        try
        {
            await entry.OpenOnceAsync(
                () => _openTransport(channel, notification => DispatchAsync(entry, notification), cancellationToken))
                .ConfigureAwait(false);
        }
        catch
        {
            await ReleaseAsync(entry, handler).ConfigureAwait(false);
            throw;
        }

        return new Handle(this, entry, handler);
    }

    /// <summary>Delivers to the handlers currently registered on a channel.</summary>
    public ValueTask DeliverAsync(string channel, RealtimeNotification notification)
    {
        ChannelEntry? entry;
        lock (_gate)
        {
            _channels.TryGetValue(channel, out entry);
        }

        return entry is null ? ValueTask.CompletedTask : DispatchAsync(entry, notification);
    }

    private async ValueTask DispatchAsync(ChannelEntry entry, RealtimeNotification notification)
    {
        Func<RealtimeNotification, ValueTask>[] snapshot;
        lock (_gate)
        {
            snapshot = [.. entry.Handlers];
        }

        foreach (var handler in snapshot)
        {
            try
            {
                await handler(notification).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Realtime handler failed for channel {Channel}", entry.Channel);
            }
        }
    }

    private async Task ReleaseAsync(ChannelEntry entry, Func<RealtimeNotification, ValueTask> handler)
    {
        IAsyncDisposable? transport = null;
        lock (_gate)
        {
            entry.Handlers.Remove(handler);
            if (entry.Handlers.Count == 0 && _channels.TryGetValue(entry.Channel, out var current) && current == entry)
            {
                _channels.Remove(entry.Channel);
                transport = entry.Transport;
            }
        }

        if (transport is not null)
        {
            await transport.DisposeAsync().ConfigureAwait(false);
        }
    }

    private sealed class ChannelEntry(string channel)
    {
        private readonly SemaphoreSlim _open = new(1, 1);

        public string Channel { get; } = channel;
        public List<Func<RealtimeNotification, ValueTask>> Handlers { get; } = [];
        public IAsyncDisposable? Transport { get; private set; }

        public async Task OpenOnceAsync(Func<Task<IAsyncDisposable>> open)
        {
            await _open.WaitAsync().ConfigureAwait(false);
            try
            {
                Transport ??= await open().ConfigureAwait(false);
            }
            finally
            {
                _open.Release();
            }
        }
    }

    private sealed class Handle(
        RealtimeNotificationFanout owner,
        ChannelEntry entry,
        Func<RealtimeNotification, ValueTask> handler) : IAsyncDisposable
    {
        private int _disposed;

        public ValueTask DisposeAsync()
            => Interlocked.Exchange(ref _disposed, 1) == 0
                ? new ValueTask(owner.ReleaseAsync(entry, handler))
                : ValueTask.CompletedTask;
    }
}
