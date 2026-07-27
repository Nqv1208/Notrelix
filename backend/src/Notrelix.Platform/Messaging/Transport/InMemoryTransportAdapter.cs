using System.Collections.Concurrent;

namespace Notrelix.Platform.Messaging.Transport;

public sealed class InMemoryTransportAdapter : ITransportAdapter
{
    private readonly ConcurrentQueue<Runtime.EventEnvelope> _envelopes = new();

    public string Name => "InMemory";

    public bool IsConnected { get; private set; } = true;

    public IReadOnlyList<Runtime.EventEnvelope> Published => _envelopes.ToList();

    public Task<TransportSendResult> SendAsync(
        Runtime.EventEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        _envelopes.Enqueue(envelope);
        return Task.FromResult(TransportSendResult.Ok());
    }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        IsConnected = true;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        IsConnected = false;
        return Task.CompletedTask;
    }

    public void Clear() => _envelopes.Clear();
}
