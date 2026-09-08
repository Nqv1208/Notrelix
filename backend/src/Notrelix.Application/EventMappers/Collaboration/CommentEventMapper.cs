using Notrelix.Application.Events.Collaboration;

namespace Notrelix.Application.EventMappers.Collaboration;

public sealed class CommentEventMapper :
    IntegrationEventMapperBase<CommentCreatedDomainEvent, CommentCreatedIntegrationEvent>,
    IIntegrationEventMapper<CommentReplyCreatedDomainEvent, CommentCreatedIntegrationEvent>,
    IIntegrationEventMapper<MentionCreatedDomainEvent, MentionCreatedIntegrationEvent>
{
    public override CommentCreatedIntegrationEvent? Map(CommentCreatedDomainEvent domainEvent)
    {
        return new CommentCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            CommentId: domainEvent.CommentId,
            WorkspaceId: domainEvent.WorkspaceId,
            TargetType: domainEvent.Target.Kind.Value,
            TargetId: domainEvent.Target.ResourceId,
            AuthorId: domainEvent.CreatedBy,
            Body: domainEvent.Content,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.CreatedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    public CommentCreatedIntegrationEvent? Map(CommentReplyCreatedDomainEvent domainEvent)
    {
        // M7 freeze: a reply is a created-comment fact on the canonical
        // "comment.created" identity — the same outward stream as the root,
        // carrying its parent identity. It is not a local-only detail.
        return new CommentCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            CommentId: domainEvent.CommentId,
            WorkspaceId: domainEvent.WorkspaceId,
            TargetType: domainEvent.Target.Kind.Value,
            TargetId: domainEvent.Target.ResourceId,
            AuthorId: domainEvent.CreatedBy,
            Body: domainEvent.Content,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.CreatedBy,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt,
            ParentCommentId: domainEvent.ParentCommentId
        );
    }

    public MentionCreatedIntegrationEvent? Map(MentionCreatedDomainEvent domainEvent)
    {
        return new MentionCreatedIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            AccountId: domainEvent.AccountId,
            MentionId: domainEvent.MentionId,
            WorkspaceId: domainEvent.WorkspaceId,
            TargetType: domainEvent.Source.Kind.Value,
            TargetId: domainEvent.Source.ResourceId,
            MentionedUserId: domainEvent.MentionedId,
            MentionedByUserId: domainEvent.MentionedByUserId,
            CorrelationId: domainEvent.EventId,
            ActorUserId: domainEvent.MentionedByUserId,
            CausationId: null,
            OccurredAt: domainEvent.OccurredAt
        );
    }

    IReadOnlyList<IntegrationEventMapping> IIntegrationEventMapper.Map(IDomainEvent domainEvent)
    {
        if (domainEvent is CommentReplyCreatedDomainEvent e0)
        {
            var mappedReply = Map(e0);
            if (mappedReply is not null) return [new IntegrationEventMapping(mappedReply)];
        }
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
