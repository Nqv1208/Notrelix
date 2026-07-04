namespace Notrelix.Application.Common.Messaging;

public interface IIntegrationEventBus
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : IIntegrationEvent;

    Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
}
