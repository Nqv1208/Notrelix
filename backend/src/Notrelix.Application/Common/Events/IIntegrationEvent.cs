namespace Notrelix.Application.Common.Events;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    Guid? SourceEventId { get; }
    string MessageName { get; }
    int SchemaVersion { get; }

    Guid? AccountId { get; }
    Guid? WorkspaceId { get; }
    Guid? ActorUserId { get; }

    Guid CorrelationId { get; }
    Guid? CausationId { get; }

    DateTimeOffset OccurredAt { get; }
}
