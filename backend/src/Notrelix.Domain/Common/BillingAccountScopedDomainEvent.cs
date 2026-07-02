namespace Notrelix.Domain.Common;

public abstract record BillingAccountScopedDomainEvent : WorkspaceScopedDomainEvent, IAccountScoped
{
    protected BillingAccountScopedDomainEvent(
        Guid accountId,
        Guid? workspaceId,
        DateTimeOffset occurredAt,
        Guid? actorUserId = null,
        string? correlationId = null,
        string? causationId = null)
        : base(accountId, workspaceId ?? accountId, occurredAt, actorUserId, correlationId, causationId)
    {
        CorrelationId = correlationId;
        CausationId = causationId;
    }
}
