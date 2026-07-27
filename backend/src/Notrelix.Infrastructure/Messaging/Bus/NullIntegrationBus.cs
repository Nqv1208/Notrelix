using Notrelix.Infrastructure.Events;

namespace Notrelix.Infrastructure.Messaging.Bus;

public sealed class NullIntegrationBus : IIntegrationBus
{
    public Task PublishAsync(EventEnvelope envelope, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
