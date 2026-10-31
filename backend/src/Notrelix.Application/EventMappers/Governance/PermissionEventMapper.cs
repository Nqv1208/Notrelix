using Notrelix.Application.Events.Governance;

namespace Notrelix.Application.EventMappers.Governance;

public sealed class PermissionEventMapper :
    IntegrationEventMapperBase<ResourcePermissionGrantedDomainEvent, ResourcePermissionGrantedIntegrationEvent>,
    IIntegrationEventMapper<ResourcePermissionRevokedDomainEvent, ResourcePermissionRevokedIntegrationEvent>
{
    public override ResourcePermissionGrantedIntegrationEvent? Map(ResourcePermissionGrantedDomainEvent domainEvent)
    {
        return new ResourcePermissionGrantedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            PermissionId: domainEvent.PermissionId,
            WorkspaceId: domainEvent.WorkspaceId,
            ResourceKind: domainEvent.ResourceKind.ToString(),
            ResourceId: domainEvent.ResourceId,
            SubjectType: domainEvent.Subject.ToString(),
            SubjectId: domainEvent.SubjectId,
            PermissionLevel: domainEvent.Level.ToString(),
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.GrantedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public ResourcePermissionRevokedIntegrationEvent? Map(ResourcePermissionRevokedDomainEvent domainEvent)
    {
        return new ResourcePermissionRevokedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            PermissionId: domainEvent.PermissionId,
            WorkspaceId: domainEvent.WorkspaceId,
            ResourceKind: domainEvent.ResourceKind.ToString(),
            ResourceId: domainEvent.ResourceId,
            SubjectType: domainEvent.Subject.ToString(),
            SubjectId: domainEvent.SubjectId,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.RevokedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    IReadOnlyList<IntegrationEventMapping> IIntegrationEventMapper.Map(IDomainEvent domainEvent)
    {
        if (domainEvent is ResourcePermissionGrantedDomainEvent e1)
        {
            var mapped = Map(e1);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is ResourcePermissionRevokedDomainEvent e2)
        {
            var mapped = Map(e2);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        return [];
    }
}
