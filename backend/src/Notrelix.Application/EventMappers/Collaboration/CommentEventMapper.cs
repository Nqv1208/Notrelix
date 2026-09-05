using Notrelix.Application.Events.Collaboration;

namespace Notrelix.Application.EventMappers.Collaboration;

public sealed class CommentEventMapper :
    IntegrationEventMapperBase<CommentCreatedDomainEvent, CommentCreatedIntegrationEvent>,
    IIntegrationEventMapper<MentionCreatedDomainEvent, MentionCreatedIntegrationEvent>
{
    public override CommentCreatedIntegrationEvent? Map(CommentCreatedDomainEvent domainEvent)
    {
        return new CommentCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            CommentId: domainEvent.CommentId,
            WorkspaceId: domainEvent.WorkspaceId,
            TargetType: domainEvent.Target.Kind.ToString(),
            TargetId: domainEvent.Target.ResourceId,
            AuthorId: domainEvent.CreatedBy,
            Body: string.Empty,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.CreatedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public MentionCreatedIntegrationEvent? Map(MentionCreatedDomainEvent domainEvent)
    {
        return new MentionCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            MentionId: domainEvent.MentionId,
            WorkspaceId: domainEvent.WorkspaceId,
            TargetType: domainEvent.Source.Kind.ToString(),
            TargetId: domainEvent.Source.ResourceId,
            MentionedUserId: domainEvent.MentionedId,
            MentionedByUserId: domainEvent.MentionedId,
            CorrelationId: domainEvent.EventId,
            ActorUserId: null,
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
