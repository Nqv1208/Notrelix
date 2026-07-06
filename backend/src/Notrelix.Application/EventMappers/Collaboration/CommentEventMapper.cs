using Notrelix.Application.Events.Collaboration;

namespace Notrelix.Application.EventMappers.Collaboration;

public sealed class CommentEventMapper :
    IntegrationEventMapperBase<CommentCreatedDomainEvent, CommentCreatedIntegrationEvent>,
    IIntegrationEventMapper<MentionCreatedDomainEvent, MentionCreatedIntegrationEvent>
{
    public override CommentCreatedIntegrationEvent? Map(CommentCreatedDomainEvent domainEvent)
    {
        return new CommentCreatedIntegrationEvent(
            domainEvent.CommentId,
            domainEvent.WorkspaceId,
            domainEvent.Target.ResourceType.ToString(),
            domainEvent.Target.ResourceId,
            domainEvent.CreatedBy,
            string.Empty,
            ((IDomainEvent)domainEvent).ActorUserId,
            default,
            null,
            domainEvent.OccurredAt
        );
    }

    public MentionCreatedIntegrationEvent? Map(MentionCreatedDomainEvent domainEvent)
    {
        return new MentionCreatedIntegrationEvent(
            domainEvent.MentionId,
            domainEvent.WorkspaceId,
            domainEvent.Source.ResourceType.ToString(),
            domainEvent.Source.ResourceId,
            domainEvent.MentionedId,
            default,
            ((IDomainEvent)domainEvent).ActorUserId,
            default,
            null,
            domainEvent.OccurredAt
        );
    }

    IReadOnlyList<IntegrationEventMapping> IIntegrationEventMapper.Map(IDomainEvent domainEvent)
    {
        if (domainEvent is CommentCreatedDomainEvent e1)
        {
            var mapped = Map(e1);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        if (domainEvent is MentionCreatedDomainEvent e2)
        {
            var mapped = Map(e2);
            if (mapped is not null) return [new IntegrationEventMapping(mapped)];
        }
        return [];
    }
}
