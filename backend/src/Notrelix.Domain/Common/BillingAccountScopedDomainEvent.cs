namespace Notrelix.Domain.Common;

public abstract record BillingAccountScopedDomainEvent : WorkspaceScopedDomainEvent, IAccountScoped
{
    public Guid AccountId { get; }

    protected BillingAccountScopedDomainEvent(
        Guid accountId,
        Guid? workspaceId,
        DateTimeOffset occurredAt,
        Guid? actorUserId = null,
        string? correlationId = null,
        string? causationId = null)
        : base(workspaceId ?? accountId, occurredAt, actorUserId, correlationId, causationId)
    {
        AccountId = accountId;
        CorrelationId = correlationId;
        CausationId = causationId;
    }
}
