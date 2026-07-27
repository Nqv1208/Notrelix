namespace Notrelix.Domain.Common;

public abstract record GlobalDomainEvent : DomainEvent
{
    protected GlobalDomainEvent(
        DateTimeOffset occurredAt)
        : base(occurredAt)
    {
    }
}
