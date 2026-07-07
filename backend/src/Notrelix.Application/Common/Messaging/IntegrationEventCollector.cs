namespace Notrelix.Application.Common.Messaging;

public sealed class IntegrationEventCollector : IIntegrationEventCollector
{
    private readonly List<IIntegrationEvent> _events = [];

    public void Add(IIntegrationEvent integrationEvent)
    {
        _events.Add(integrationEvent);
    }

    public IReadOnlyCollection<IIntegrationEvent> DequeueAll()
    {
        var events = _events.ToArray();
        _events.Clear();
        return events;
    }
}