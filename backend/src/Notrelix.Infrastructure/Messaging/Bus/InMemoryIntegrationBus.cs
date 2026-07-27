using System.Collections.Concurrent;
using Notrelix.Infrastructure.Events;

namespace Notrelix.Infrastructure.Messaging.Bus;

public sealed class InMemoryIntegrationBus : IIntegrationBus
{
    private readonly ConcurrentQueue<EventEnvelope> _envelopes = new();

    public IReadOnlyList<EventEnvelope> Published => _envelopes.ToList();

    public Task PublishAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        _envelopes.Enqueue(envelope);
        return Task.CompletedTask;
    }

    public void Clear() => _envelopes.Clear();
}
