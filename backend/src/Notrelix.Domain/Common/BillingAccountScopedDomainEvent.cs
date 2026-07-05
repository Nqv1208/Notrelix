namespace Notrelix.Domain.Common;

public abstract record BillingAccountScopedDomainEvent : DomainEvent, IAccountScoped
{
    public Guid AccountId { get; }
    public Guid? WorkspaceId { get; }

    protected BillingAccountScopedDomainEvent(
        Guid accountId,
        Guid? workspaceId,
        DateTimeOffset occurredAt,
        Guid? actorUserId = null,
        string? correlationId = null,
        string? causationId = null)
        : base(occurredAt, workspaceId, actorUserId)
    {
        AccountId = accountId;
        WorkspaceId = workspaceId;
        CorrelationId = correlationId;
        CausationId = causationId;
    }
}
