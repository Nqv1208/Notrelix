namespace Notrelix.Application.Common.Messaging;

public sealed class IntegrationEventCollector : IIntegrationEventCollector
{
    private readonly List<IIntegrationEvent> _events = [];

    public void Add(IIntegrationEvent integrationEvent)
    {
        _events.Add(integrationEvent);
    }

    public IntegrationEventBatch CapturePending()
    {
        return new IntegrationEventBatch(Guid.NewGuid(), _events.ToArray());
    }

    public void Acknowledge(IntegrationEventBatch batch)
    {
        foreach (var evt in batch.Events)
        {
            _events.Remove(evt);
        }
    }

    public void Restore(IntegrationEventBatch batch)
    {
        foreach (var evt in batch.Events)
        {
            if (!_events.Contains(evt))
            {
                _events.Add(evt);
            }
        }
    }
}
