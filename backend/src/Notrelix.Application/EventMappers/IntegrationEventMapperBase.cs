using Notrelix.Application.Common.Events;

namespace Notrelix.Application.EventMappers;

public abstract class IntegrationEventMapperBase<TDomainEvent, TIntegrationEvent> : IIntegrationEventMapper<TDomainEvent, TIntegrationEvent>
    where TDomainEvent : IDomainEvent
    where TIntegrationEvent : IIntegrationEvent
{
    public abstract TIntegrationEvent? Map(TDomainEvent domainEvent);

    IReadOnlyList<IntegrationEventMapping> IIntegrationEventMapper.Map(IDomainEvent domainEvent)
    {
        if (domainEvent is TDomainEvent typedEvent)
        {
            var result = Map(typedEvent);
            if (result is not null)
            {
                return [new IntegrationEventMapping(result)];
            }
        }
        return [];
    }
}
