namespace Notrelix.Application.Common.Messaging;

public interface IIntegrationEventCollector
{
    void Add(IIntegrationEvent integrationEvent);
    IReadOnlyCollection<IIntegrationEvent> DequeueAll();
}