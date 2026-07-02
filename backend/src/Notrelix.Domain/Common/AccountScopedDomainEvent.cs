namespace Notrelix.Domain.Common;

public abstract record AccountScopedDomainEvent : WorkspaceScopedDomainEvent, IAccountScoped
{
    protected AccountScopedDomainEvent(
        Guid accountId,
        DateTimeOffset occurredAt,
        Guid? actorUserId = null,
        string? correlationId = null,
        string? causationId = null)
        : base(accountId, accountId, occurredAt, actorUserId, correlationId, causationId)
    {
        CorrelationId = correlationId;
        CausationId = causationId;
    }
}
