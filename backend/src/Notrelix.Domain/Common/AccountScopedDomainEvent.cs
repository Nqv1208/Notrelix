namespace Notrelix.Domain.Common;

public abstract record AccountScopedDomainEvent : DomainEvent
{
    public Guid AccountId { get; }

    protected AccountScopedDomainEvent(
        Guid accountId,
        DateTimeOffset occurredAt)
        : base(occurredAt)
    {
        AccountId = accountId;
    }
}
