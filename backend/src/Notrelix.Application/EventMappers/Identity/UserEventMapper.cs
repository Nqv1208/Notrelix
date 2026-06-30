using Notrelix.Application.Events.Identity;
using Notrelix.Domain.Common;
using Notrelix.Domain.Identity.Users.Events;

namespace Notrelix.Application.EventMappers.Identity;

public sealed class UserEventMapper :
    IntegrationEventMapperBase<UserRegisteredDomainEvent, UserRegisteredIntegrationEvent>,
    IIntegrationEventMapper<UserDeactivatedDomainEvent, UserDeactivatedIntegrationEvent>
{
    public override UserRegisteredIntegrationEvent? Map(UserRegisteredDomainEvent domainEvent)
    {
        var de = (IDomainEvent)domainEvent;
        return new UserRegisteredIntegrationEvent(
            UserId: domainEvent.UserId,
            Email: domainEvent.Email,
            DisplayName: domainEvent.DisplayName,
            ActorUserId: de.ActorUserId,
            SourceEventId: de.EventId,
            CorrelationId: de.CorrelationId,
            CausationId: de.CausationId ?? de.EventId.ToString(),
            OccurredAt: domainEvent.RegisteredAt
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
        return [];
    }
}
