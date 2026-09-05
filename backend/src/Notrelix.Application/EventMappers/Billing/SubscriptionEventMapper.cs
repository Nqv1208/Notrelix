using Notrelix.Application.Events.Billing;

namespace Notrelix.Application.EventMappers.Billing;

public sealed class SubscriptionEventMapper :
    IntegrationEventMapperBase<SubscriptionChangedDomainEvent, SubscriptionChangedIntegrationEvent>,
    IIntegrationEventMapper<SubscriptionCanceledDomainEvent, SubscriptionCanceledIntegrationEvent>
{
    public override SubscriptionChangedIntegrationEvent? Map(SubscriptionChangedDomainEvent domainEvent)
    {
        return new SubscriptionChangedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            SubscriptionId: domainEvent.SubscriptionId,
            WorkspaceId: domainEvent.WorkspaceId,
            PreviousPlanId: domainEvent.OldPlanId,
            NewPlanId: domainEvent.NewPlanId,
            CorrelationId: domainEvent.EventId,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public SubscriptionCanceledIntegrationEvent? Map(SubscriptionCanceledDomainEvent domainEvent)
    {
        return new SubscriptionCanceledIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            SubscriptionId: domainEvent.SubscriptionId,
            WorkspaceId: domainEvent.WorkspaceId,
            EffectiveAt: domainEvent.OccurredAt,
            CorrelationId: domainEvent.EventId,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
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
