using Notrelix.Application.Events.Billing;

namespace Notrelix.Application.EventMappers.Billing;

public sealed class SubscriptionEventMapper :
    IntegrationEventMapperBase<SubscriptionChangedDomainEvent, SubscriptionChangedIntegrationEvent>,
    IIntegrationEventMapper<SubscriptionCanceledDomainEvent, SubscriptionCanceledIntegrationEvent>
{
    public override SubscriptionChangedIntegrationEvent? Map(SubscriptionChangedDomainEvent domainEvent)
    {
        return new SubscriptionChangedIntegrationEvent(
            domainEvent.SubscriptionId,
            domainEvent.WorkspaceId,
            domainEvent.OldPlanId,
            domainEvent.NewPlanId,
            default,
            null,
            domainEvent.OccurredAt
        );
    }

    public SubscriptionCanceledIntegrationEvent? Map(SubscriptionCanceledDomainEvent domainEvent)
    {
        return new SubscriptionCanceledIntegrationEvent(
            domainEvent.SubscriptionId,
            ((IDomainEvent)domainEvent).WorkspaceId ?? Guid.Empty,
            domainEvent.OccurredAt,
            default,
            null,
            domainEvent.OccurredAt
        );
    }

    IReadOnlyList<IntegrationEventMapping> IIntegrationEventMapper.Map(IDomainEvent domainEvent)
    {
        if (domainEvent is SubscriptionChangedDomainEvent e1)
        {
            var mapped = Map(e1);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is SubscriptionCanceledDomainEvent e2)
        {
            var mapped = Map(e2);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        return [];
    }
}
