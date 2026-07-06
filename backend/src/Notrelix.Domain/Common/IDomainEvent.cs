namespace Notrelix.Domain.Common;

public interface IDomainEvent
{
    Guid EventId { get; }
    int EventVersion { get; }
    DateTimeOffset OccurredAt { get; }

    string SourceContext { get; }
    string AggregateType { get; }
    Guid AggregateId { get; }
    string SubjectType { get; }
    Guid SubjectId { get; }

    Guid? WorkspaceId { get; }
    Guid? ActorUserId { get; }
    string? CorrelationId { get; }
    string? CausationId { get; }
}
