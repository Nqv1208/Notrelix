namespace Notrelix.Domain.Common;

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; }
    public DateTimeOffset OccurredAt { get; }

    protected DomainEvent(DateTimeOffset occurredAt)
    {
        if (occurredAt == default)
            throw new ArgumentOutOfRangeException(nameof(occurredAt));
        EventId = Guid.CreateVersion7();
        OccurredAt = occurredAt;
    }
}
