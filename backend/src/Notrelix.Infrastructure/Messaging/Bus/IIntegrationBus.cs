using Notrelix.Infrastructure.Events;

namespace Notrelix.Infrastructure.Messaging.Bus;

public interface IIntegrationBus
{
    Task PublishAsync(EventEnvelope envelope, CancellationToken cancellationToken = default);
}
