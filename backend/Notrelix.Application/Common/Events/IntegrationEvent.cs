namespace Notrelix.Application.Common.Events;

public abstract record IntegrationEvent : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.CreateVersion7();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
    public string EventType => GetType().FullName!;
}
