using Notrelix.Application.Events.Governance;

namespace Notrelix.Application.EventMappers.Governance;

public sealed class PermissionEventMapper :
    IntegrationEventMapperBase<ResourcePermissionGrantedDomainEvent, ResourcePermissionGrantedIntegrationEvent>,
    IIntegrationEventMapper<ResourcePermissionRevokedDomainEvent, ResourcePermissionRevokedIntegrationEvent>,
    IIntegrationEventMapper<CustomRoleAssignedDomainEvent, CustomRoleAssignedIntegrationEvent>
{
    public override ResourcePermissionGrantedIntegrationEvent? Map(ResourcePermissionGrantedDomainEvent domainEvent)
    {
        return new ResourcePermissionGrantedIntegrationEvent(
            domainEvent.PermissionId,
            domainEvent.WorkspaceId,
            domainEvent.ResourceType.ToString(),
            domainEvent.ResourceId,
            domainEvent.SubjectType.ToString(),
            domainEvent.SubjectId,
            domainEvent.Level.ToString(),
            ((IDomainEvent)domainEvent).ActorUserId,
            null,
            null,
            domainEvent.OccurredAt
        );
    }

    public ResourcePermissionRevokedIntegrationEvent? Map(ResourcePermissionRevokedDomainEvent domainEvent)
    {
        return new ResourcePermissionRevokedIntegrationEvent(
            domainEvent.PermissionId,
            domainEvent.WorkspaceId,
            domainEvent.ResourceType.ToString(),
            domainEvent.ResourceId,
            domainEvent.SubjectType.ToString(),
            domainEvent.SubjectId,
            ((IDomainEvent)domainEvent).ActorUserId,
            null,
            null,
            domainEvent.OccurredAt
        );
    }

    public CustomRoleAssignedIntegrationEvent? Map(CustomRoleAssignedDomainEvent domainEvent)
    {
        return new CustomRoleAssignedIntegrationEvent(
            domainEvent.RoleId,
            domainEvent.WorkspaceId,
            string.Empty,
            domainEvent.MemberId,
            ((IDomainEvent)domainEvent).ActorUserId,
            null,
            null,
            domainEvent.OccurredAt
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
        if (domainEvent is CustomRoleAssignedDomainEvent e3)
        {
            var mapped = Map(e3);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        return [];
    }
}
