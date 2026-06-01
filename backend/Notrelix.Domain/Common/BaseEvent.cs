namespace Notrelix.Domain.Common;

// Interface cho Domain Event
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
}

// Base class cho Domain Event
public abstract class BaseEvent : IDomainEvent
{
    public Guid EventId { get; }
    public DateTime OccurredOn { get; }

    protected BaseEvent()
    {
        EventId = Guid.CreateVersion7();
        OccurredOn = DateTime.UtcNow;
    }
}