using Notrelix.Application.Common.Events;

namespace Notrelix.Application.Common.Abstractions;

public interface IIntegrationEventBus
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : IIntegrationEvent;
}
