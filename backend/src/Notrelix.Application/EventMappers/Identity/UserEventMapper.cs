using Notrelix.Application.Events.Identity;
using Notrelix.Domain.Accounts.Accounts.Events;
using Notrelix.Domain.Identity.Users.Events;

namespace Notrelix.Application.EventMappers.Identity;

public sealed class UserEventMapper :
    IntegrationEventMapperBase<UserRegisteredDomainEvent, UserRegisteredIntegrationEvent>,
    IIntegrationEventMapper<UserDeactivatedDomainEvent, UserDeactivatedIntegrationEvent>,
    IIntegrationEventMapper<AccountCreatedDomainEvent, UserRegisteredIntegrationEvent>
{
    public override UserRegisteredIntegrationEvent? Map(UserRegisteredDomainEvent domainEvent)
    {
        var de = (IDomainEvent)domainEvent;
        return new UserRegisteredIntegrationEvent(
            UserId: domainEvent.UserId,
            AccountId: Guid.Empty, // Will be populated by AccountCreatedDomainEvent mapping
            Email: domainEvent.Email,
            DisplayName: domainEvent.DisplayName,
            ActorUserId: de.ActorUserId,
            SourceEventId: de.EventId,
            CorrelationId: de.CorrelationId,
            CausationId: de.CausationId ?? de.EventId.ToString(),
            OccurredAt: domainEvent.RegisteredAt
        );
    }

    public UserRegisteredIntegrationEvent? Map(AccountCreatedDomainEvent domainEvent)
    {
        var de = (IDomainEvent)domainEvent;
        return new UserRegisteredIntegrationEvent(
            UserId: domainEvent.CreatedBy,
            AccountId: domainEvent.AccountId,
            Email: string.Empty, // Not available in AccountCreatedDomainEvent
            DisplayName: domainEvent.Name,
            ActorUserId: de.ActorUserId,
            SourceEventId: de.EventId,
            CorrelationId: de.CorrelationId,
            CausationId: de.CausationId ?? de.EventId.ToString(),
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public UserDeactivatedIntegrationEvent? Map(UserDeactivatedDomainEvent domainEvent)
    {
        var de = (IDomainEvent)domainEvent;
        return new UserDeactivatedIntegrationEvent(
            domainEvent.UserId,
            domainEvent.DeactivatedBy,
            de.CorrelationId,
            de.CausationId,
            domainEvent.DeactivatedAt
        );
    }

    IReadOnlyList<IntegrationEventMapping> IIntegrationEventMapper.Map(IDomainEvent domainEvent)
    {
        if (domainEvent is UserRegisteredDomainEvent e1)
        {
            var mapped = Map(e1);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is UserDeactivatedDomainEvent e2)
        {
            var mapped = Map(e2);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is AccountCreatedDomainEvent e3)
        {
            var mapped = Map(e3);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        return [];
    }
}
