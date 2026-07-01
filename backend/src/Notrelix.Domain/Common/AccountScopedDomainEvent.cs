namespace Notrelix.Domain.Common;

public abstract record AccountScopedDomainEvent : WorkspaceScopedDomainEvent, IAccountScoped
{
    public Guid AccountId { get; }

    protected AccountScopedDomainEvent(
        Guid accountId,
        DateTimeOffset occurredAt,
        Guid? actorUserId = null,
        string? correlationId = null,
        string? causationId = null)
        : base(accountId, occurredAt, actorUserId, correlationId, causationId)
    {
        AccountId = accountId;
        CorrelationId = correlationId;
        CausationId = causationId;
    }
}
