namespace Notrelix.Application.Common.Events;

public sealed record IntegrationEventMapping(IIntegrationEvent IntegrationEvent);

public interface IIntegrationEventMapper
{
    IReadOnlyList<IntegrationEventMapping> Map(IDomainEvent domainEvent);
}

public interface IIntegrationEventMapper<in TDomainEvent, TIntegrationEvent> : IIntegrationEventMapper
    where TDomainEvent : IDomainEvent
    where TIntegrationEvent : IIntegrationEvent
{
    TIntegrationEvent? Map(TDomainEvent domainEvent);
}
