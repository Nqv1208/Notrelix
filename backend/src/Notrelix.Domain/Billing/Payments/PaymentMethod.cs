using Notrelix.Domain.Billing.Payments.Events;

namespace Notrelix.Domain.Billing.Payments;

public class PaymentMethod : SoftDeletableAggregateRoot, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public PaymentProvider Provider { get; private set; }
    public string ProviderMethodId { get; private set; } = null!;
    public string Last4 { get; private set; } = null!;
    public string Brand { get; private set; } = null!;
    public PaymentMethodStatus Status { get; private set; }
    public bool IsDefault { get; private set; }

    private PaymentMethod() : base() { }

    public static PaymentMethod Create(Guid accountId, Guid workspaceId, PaymentProvider provider, string providerMethodId, string last4, string brand, Guid createdBy, DateTimeOffset createdAt, bool isDefault = false)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(providerMethodId);
        Guard.NotEmpty(createdBy);

        var method = new PaymentMethod
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            Provider = provider,
            ProviderMethodId = providerMethodId,
            Last4 = last4,
            Brand = brand,
            Status = PaymentMethodStatus.Active,
            IsDefault = isDefault
        };

        method.SetAuditOnCreate(createdBy, createdAt);
        method.RaiseDomainEvent(new PaymentMethodAddedDomainEvent(accountId, workspaceId, method.Id, provider, last4, brand, createdAt));
        return method;
    }

    public void SetAsDefault(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (IsDefault) return;

        IsDefault = true;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public void UnsetAsDefault(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (!IsDefault) return;

        IsDefault = false;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public void Deactivate(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status == PaymentMethodStatus.Expired) return;

        Status = PaymentMethodStatus.Expired;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public void Reactivate(Guid updatedBy, DateTimeOffset updatedAt)
    {
        EnsureNotDeleted();
        if (Status == PaymentMethodStatus.Active) return;

        Status = PaymentMethodStatus.Active;
        SetAuditOnUpdate(updatedBy, updatedAt);
        IncrementVersion();
    }

    public void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        if (!MarkDeleted(deletedBy, deletedAt, reason)) return;
        SetAuditOnUpdate(deletedBy, deletedAt);
        IncrementVersion();
    }

    public void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        if (!MarkRestored(restoredBy, restoredAt)) return;
        SetAuditOnUpdate(restoredBy, restoredAt);
        IncrementVersion();
    }
}
