using System.Collections.Concurrent;
using System.Text.Json;

using Notrelix.Application.Common.Realtime.Gateway;

namespace Notrelix.Infrastructure.Realtime;

/// <summary>Moves one ephemeral message (a JSON string) between processes by channel name.</summary>
public interface IRealtimeEphemeralTransport
{
    Task PublishAsync(string channel, string message);

    Task<IAsyncDisposable> OpenAsync(string channel, Action<string> onMessage);
}

/// <summary>
/// Ephemeral signal bus over a transport (<see cref="RedisRealtimeEphemeralTransport"/> or the in-process one). One
/// transport subscription per channel per process, shared by every connection of the scope (ref-counted); a message
/// that does not parse is dropped. Nothing is stored or replayed.
/// </summary>
public sealed class RealtimeEphemeralBus : IRealtimeEphemeralBus
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly IRealtimeEphemeralTransport _transport;
    private readonly ILogger<RealtimeEphemeralBus> _logger;
    private readonly object _gate = new();
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    public RealtimeEphemeralBus(IRealtimeEphemeralTransport transport, ILogger<RealtimeEphemeralBus> logger)
    {
        _transport = transport;
        _logger = logger;
    }

    public int OpenChannelCount
    {
        get
        {
            lock (_gate)
            {
                return _entries.Count;
            }
        }
    }

    /// <summary>Ephemeral signals travel on their own channel so they never touch the gap-free event stream of the scope.</summary>
    public static string ChannelOf(RealtimeScope scope) => $"ephemeral:{scope.WireKind}:{scope.Id}";

    public Task PublishAsync(RealtimeScope scope, RealtimeEphemeralSignal signal, CancellationToken cancellationToken)
        => _transport.PublishAsync(
            RealtimeChannelResolver.RedisChannel(ChannelOf(scope)),
            JsonSerializer.Serialize(signal, Json));

    public async Task<IAsyncDisposable> SubscribeAsync(
        RealtimeScope scope,
        Func<RealtimeEphemeralSignal, ValueTask> onSignal,
        CancellationToken cancellationToken)
    {
        var channel = ChannelOf(scope);
        Entry entry;
        lock (_gate)
        {
            if (!_entries.TryGetValue(channel, out entry!))
            {
                entry = new Entry(channel);
                _entries[channel] = entry;
            }

            entry.Handlers.Add(onSignal);
        }

        try
        {
            await entry.OpenOnceAsync(() => _transport.OpenAsync(
                RealtimeChannelResolver.RedisChannel(channel), json => Dispatch(entry, json))).ConfigureAwait(false);
        }
        catch
        {
            await ReleaseAsync(entry, onSignal).ConfigureAwait(false);
            throw;
        }

        return new Handle(this, entry, onSignal);
    }

    private void Dispatch(Entry entry, string json)
    {
        RealtimeEphemeralSignal? signal;
        try
        {
            signal = JsonSerializer.Deserialize<RealtimeEphemeralSignal>(json, Json);
        }
        catch (JsonException)
        {
            signal = null;
        }

        if (signal is null || string.IsNullOrEmpty(signal.Kind) || signal.WorkspaceId == Guid.Empty || signal.UserId == Guid.Empty)
        {
            _logger.LogWarning("Dropped malformed ephemeral realtime message on channel {Channel}", entry.Channel);
            return;
        }

        Func<RealtimeEphemeralSignal, ValueTask>[] snapshot;
        lock (_gate)
        {
            snapshot = [.. entry.Handlers];
        }

        foreach (var handler in snapshot)
        {
            try
            {
                // Handlers enqueue and never block the transport callback.
                _ = handler(signal);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ephemeral realtime handler failed for channel {Channel}", entry.Channel);
            }
        }
    }

    private async Task ReleaseAsync(Entry entry, Func<RealtimeEphemeralSignal, ValueTask> handler)
    {
        IAsyncDisposable? transport = null;
        lock (_gate)
        {
            entry.Handlers.Remove(handler);
            if (entry.Handlers.Count == 0 && _entries.TryGetValue(entry.Channel, out var current) && current == entry)
            {
                _entries.Remove(entry.Channel);
                transport = entry.Transport;
            }
        }

        if (transport is not null)
        {
            await transport.DisposeAsync().ConfigureAwait(false);
        }
    }

    private sealed class Entry(string channel)
    {
        private readonly SemaphoreSlim _open = new(1, 1);

        public string Channel { get; } = channel;
        public List<Func<RealtimeEphemeralSignal, ValueTask>> Handlers { get; } = [];
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

    private sealed class Handle(RealtimeEphemeralBus owner, Entry entry, Func<RealtimeEphemeralSignal, ValueTask> handler) : IAsyncDisposable
    {
        private int _disposed;

        public ValueTask DisposeAsync()
            => Interlocked.Exchange(ref _disposed, 1) == 0
                ? new ValueTask(owner.ReleaseAsync(entry, handler))
                : ValueTask.CompletedTask;
    }
}

/// <summary>Single-process transport (Development, Testing): delivers to the openers of a channel in this process.</summary>
public sealed class InProcessRealtimeEphemeralTransport : IRealtimeEphemeralTransport
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Action<string>>> _channels = new(StringComparer.Ordinal);

    public Task PublishAsync(string channel, string message)
    {
        if (_channels.TryGetValue(channel, out var listeners))
        {
            foreach (var listener in listeners.Values)
            {
                listener(message);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IAsyncDisposable> OpenAsync(string channel, Action<string> onMessage)
    {
        var id = Guid.NewGuid();
        _channels.GetOrAdd(channel, _ => new()).TryAdd(id, onMessage);
        return Task.FromResult<IAsyncDisposable>(new Registration(this, channel, id));
    }

    private sealed class Registration(InProcessRealtimeEphemeralTransport owner, string channel, Guid id) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            if (owner._channels.TryGetValue(channel, out var listeners))
            {
                listeners.TryRemove(id, out _);
                if (listeners.IsEmpty)
                {
                    owner._channels.TryRemove(new KeyValuePair<string, ConcurrentDictionary<Guid, Action<string>>>(channel, listeners));
                }
            }

            return ValueTask.CompletedTask;
        }
    }
}

/// <summary>Redis pub/sub transport: fire and forget, no stream counter, nothing retained.</summary>
public sealed class RedisRealtimeEphemeralTransport(IConnectionMultiplexer redis) : IRealtimeEphemeralTransport
{
    public async Task PublishAsync(string channel, string message)
        => await redis.GetSubscriber().PublishAsync(RedisChannel.Literal(channel), message, CommandFlags.FireAndForget)
            .ConfigureAwait(false);

    public async Task<IAsyncDisposable> OpenAsync(string channel, Action<string> onMessage)
    {
        // A message queue hands messages over one at a time in arrival order.
        var queue = await redis.GetSubscriber().SubscribeAsync(RedisChannel.Literal(channel)).ConfigureAwait(false);
        queue.OnMessage(message => onMessage(message.Message.ToString()));
        return new QueueSubscription(queue);
    }

    private sealed class QueueSubscription(ChannelMessageQueue queue) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() => await queue.UnsubscribeAsync().ConfigureAwait(false);
    }
}
