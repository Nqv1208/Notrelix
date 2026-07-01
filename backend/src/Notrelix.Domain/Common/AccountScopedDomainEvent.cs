namespace Notrelix.Domain.Common;

public abstract record AccountScopedDomainEvent : DomainEvent, IAccountScoped
{
    public Guid AccountId { get; }

    protected AccountScopedDomainEvent(
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
