namespace Notrelix.Domain.Billing.Common;

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
        if (accountId == Guid.Empty)
            throw new ArgumentException("Account id cannot be empty.", nameof(accountId));
        AccountId = accountId;
        WorkspaceId = workspaceId;
    }
}
