using Notrelix.Application.Events.Identity;
using Notrelix.Domain.Identity.Users.Events;

namespace Notrelix.Application.EventMappers.Identity;

public sealed class UserEventMapper :
    IntegrationEventMapperBase<UserRegisteredDomainEvent, UserRegisteredIntegrationEvent>,
    IIntegrationEventMapper<UserDeactivatedDomainEvent, UserDeactivatedIntegrationEvent>
{
    public override UserRegisteredIntegrationEvent? Map(UserRegisteredDomainEvent domainEvent)
    {
        return new UserRegisteredIntegrationEvent(
            domainEvent.UserId,
            domainEvent.Email.Value,
            string.Empty,
            ((IDomainEvent)domainEvent).ActorUserId,
            null,
            null,
            domainEvent.OccurredAt
        );
    }

    public UserDeactivatedIntegrationEvent? Map(UserDeactivatedDomainEvent domainEvent)
    {
        return new UserDeactivatedIntegrationEvent(
            domainEvent.UserId,
            ((IDomainEvent)domainEvent).ActorUserId,
            null,
            null,
            domainEvent.OccurredAt
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
        return [];
    }
}
