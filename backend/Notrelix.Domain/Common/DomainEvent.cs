namespace Notrelix.Domain.Common;

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredAt { get; }

    protected DomainEvent(DateTimeOffset occurredAt)
    {
        EventId = Guid.NewGuid();
        OccurredAt = occurredAt;
    }
}
