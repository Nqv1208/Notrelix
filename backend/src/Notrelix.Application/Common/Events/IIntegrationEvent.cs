namespace Notrelix.Application.Common.Events;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    Guid? SourceEventId { get; }
    string MessageName { get; }
    int SchemaVersion { get; }
    Guid? WorkspaceId { get; }
    Guid? ActorUserId { get; }
    string? CorrelationId { get; }
    string? CausationId { get; }
    DateTimeOffset OccurredAt { get; }
}
