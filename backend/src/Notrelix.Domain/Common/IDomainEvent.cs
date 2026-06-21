namespace Notrelix.Domain.Common;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
    Guid? WorkspaceId { get; }
    Guid? ActorUserId { get; }
    string? CorrelationId { get; }
    string? CausationId { get; }
    int EventVersion { get; }
}
