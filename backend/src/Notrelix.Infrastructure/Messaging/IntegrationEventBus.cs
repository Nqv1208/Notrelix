namespace Notrelix.Infrastructure.Messaging;

public sealed class IntegrationEventBus : IIntegrationEventBus
{
    private readonly IPublishEndpoint _publishEndpoint;

    public IntegrationEventBus(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
        where T : IIntegrationEvent
    {
        await _publishEndpoint.Publish(integrationEvent, cancellationToken);
    }

    public async Task PublishAsync(IIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        await _publishEndpoint.Publish(integrationEvent, cancellationToken);
    }
}
