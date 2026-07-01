namespace Notrelix.Domain.Common;

public abstract record AccountRootDomainEvent : DomainEvent
{
    public Guid AccountId { get; }

    protected AccountRootDomainEvent(
        Guid accountId,
        DateTimeOffset occurredAt,
        Guid? actorUserId = null,
        string? correlationId = null,
        string? causationId = null)
        : base(occurredAt, accountId, actorUserId)
    {
        AccountId = accountId;
        CorrelationId = correlationId;
        CausationId = causationId;
    }
}
