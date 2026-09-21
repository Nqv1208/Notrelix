using Notrelix.Application.Events.Integrations;
using Notrelix.Domain.Integrations.Connections.Events;

namespace Notrelix.Application.EventMappers.Integrations;

public sealed class IntegrationConnectionEventMapper :
    IIntegrationEventMapper<IntegrationConnectionRevokedDomainEvent, IntegrationConnectionRevokedIntegrationEvent>
{
    public IntegrationConnectionRevokedIntegrationEvent? Map(IntegrationConnectionRevokedDomainEvent domainEvent) =>
        new(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            WorkspaceId: domainEvent.WorkspaceId,
            ConnectionId: domainEvent.ConnectionId,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.RevokedBy,
            OccurredAt: domainEvent.OccurredAt);

    IReadOnlyList<IntegrationEventMapping> IIntegrationEventMapper.Map(IDomainEvent domainEvent)
    {
        if (domainEvent is IntegrationConnectionRevokedDomainEvent revoked)
            return [new IntegrationEventMapping(Map(revoked)!)];

        return [];
    }
}
