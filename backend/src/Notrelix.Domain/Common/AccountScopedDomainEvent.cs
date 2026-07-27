namespace Notrelix.Domain.Common;

public abstract record AccountScopedDomainEvent : DomainEvent, IAccountScoped
{
    public Guid AccountId { get; }

    protected AccountScopedDomainEvent(
        Guid accountId,
        DateTimeOffset occurredAt)
        : base(occurredAt)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException("Account id cannot be empty.", nameof(accountId));
        AccountId = accountId;
    }
}
