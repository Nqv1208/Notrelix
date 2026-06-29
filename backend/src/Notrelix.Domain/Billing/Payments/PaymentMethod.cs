namespace Notrelix.Domain.Billing.Payments;

public class PaymentMethod : AggregateRoot, IWorkspaceScoped
{
    public Guid WorkspaceId { get; private set; }
    public PaymentProvider Provider { get; private set; }
    public string ProviderMethodId { get; private set; } = null!;
    public string Last4 { get; private set; } = null!;
    public string Brand { get; private set; } = null!;
    public PaymentMethodStatus Status { get; private set; }
    public bool IsDefault { get; private set; }

    private PaymentMethod() : base() { }

    public static PaymentMethod Create(Guid workspaceId, PaymentProvider provider, string providerMethodId, string last4, string brand, Guid createdBy, DateTimeOffset createdAt, bool isDefault = false)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(providerMethodId);

        var method = new PaymentMethod
        {
            WorkspaceId = workspaceId,
            Provider = provider,
            ProviderMethodId = providerMethodId,
            Last4 = last4,
            Brand = brand,
            Status = PaymentMethodStatus.Active,
            IsDefault = isDefault
        };

        method.SetAuditOnCreate(createdBy, createdAt);
        method.AddDomainEvent(new PaymentMethodAddedDomainEvent(workspaceId, method.Id, provider, last4, brand, createdAt));
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

    public override void SoftDelete(Guid deletedBy, DateTimeOffset deletedAt, string? reason = null)
    {
        if (IsDeleted) return;
        base.SoftDelete(deletedBy, deletedAt, reason);
        IncrementVersion();
    }

    public override void Restore(Guid restoredBy, DateTimeOffset restoredAt)
    {
        if (!IsDeleted) return;
        base.Restore(restoredBy, restoredAt);
        IncrementVersion();
    }
}
