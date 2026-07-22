namespace Notrelix.Domain.Common;

public abstract record BillingAccountScopedDomainEvent : DomainEvent, IAccountScoped
{
    public Guid AccountId { get; }
    public Guid? WorkspaceId { get; }

    protected BillingAccountScopedDomainEvent(
        Guid accountId,
        Guid? workspaceId,
        DateTimeOffset occurredAt)
        : base(occurredAt)
    {
        AccountId = accountId;
        WorkspaceId = workspaceId;
    }
}
