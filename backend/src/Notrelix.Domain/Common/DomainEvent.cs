namespace Notrelix.Domain.Common;

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; protected set; }
    public DateTimeOffset OccurredAt { get; protected set; }

    protected DomainEvent()
    {
        EventId = Guid.CreateVersion7();
        OccurredAt = DateTimeOffset.UtcNow;
    }

    protected DomainEvent(DateTimeOffset occurredAt)
    {
        EventId = Guid.CreateVersion7();
        OccurredAt = occurredAt;
    }
}
