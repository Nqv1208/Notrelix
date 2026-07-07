using Notrelix.Application.Events.Identity;
using Notrelix.Domain.Identity.Users.Events;

namespace Notrelix.Application.EventMappers.Identity;

public sealed class UserEventMapper :
    IIntegrationEventMapper<UserDeactivatedDomainEvent, UserDeactivatedIntegrationEvent>
{
    public UserDeactivatedIntegrationEvent? Map(UserDeactivatedDomainEvent domainEvent)
    {
        var de = (IDomainEvent)domainEvent;
        var correlationId = Guid.TryParse(de.CorrelationId, out var corrId) ? corrId : Guid.CreateVersion7();
        return new UserDeactivatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            UserId: domainEvent.UserId,
            CorrelationId: correlationId,
            ActorUserId: domainEvent.DeactivatedBy,
            CausationId: Guid.TryParse(de.CausationId, out var causId) ? causId : null,
            OccurredAt: domainEvent.DeactivatedAt
        );
    }

    IReadOnlyList<IntegrationEventMapping> IIntegrationEventMapper.Map(IDomainEvent domainEvent)
    {
        if (domainEvent is UserDeactivatedDomainEvent e)
        {
            var mapped = Map(e);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        return [];
    }
}
