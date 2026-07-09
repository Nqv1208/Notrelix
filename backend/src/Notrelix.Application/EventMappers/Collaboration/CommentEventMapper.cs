using Notrelix.Application.Events.Collaboration;

namespace Notrelix.Application.EventMappers.Collaboration;

public sealed class CommentEventMapper :
    IntegrationEventMapperBase<CommentCreatedDomainEvent, CommentCreatedIntegrationEvent>,
    IIntegrationEventMapper<MentionCreatedDomainEvent, MentionCreatedIntegrationEvent>
{
    public override CommentCreatedIntegrationEvent? Map(CommentCreatedDomainEvent domainEvent)
    {
        var de = (IDomainEvent)domainEvent;
        var correlationId = Guid.TryParse(de.CorrelationId, out var c) ? c : Guid.CreateVersion7();
        return new CommentCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            CommentId: domainEvent.CommentId,
            WorkspaceId: domainEvent.WorkspaceId,
            TargetType: domainEvent.Target.ResourceType.ToString(),
            TargetId: domainEvent.Target.ResourceId,
            AuthorId: domainEvent.CreatedBy,
            Body: string.Empty,
            CorrelationId: correlationId,
            ActorUserId: de.ActorUserId,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public MentionCreatedIntegrationEvent? Map(MentionCreatedDomainEvent domainEvent)
    {
        var de = (IDomainEvent)domainEvent;
        var correlationId = Guid.TryParse(de.CorrelationId, out var c) ? c : Guid.CreateVersion7();
        return new MentionCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            MentionId: domainEvent.MentionId,
            WorkspaceId: domainEvent.WorkspaceId,
            TargetType: domainEvent.Source.ResourceType.ToString(),
            TargetId: domainEvent.Source.ResourceId,
            MentionedUserId: domainEvent.MentionedId,
            MentionedByUserId: de.ActorUserId ?? domainEvent.MentionedId,
            CorrelationId: correlationId,
            ActorUserId: de.ActorUserId,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
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
