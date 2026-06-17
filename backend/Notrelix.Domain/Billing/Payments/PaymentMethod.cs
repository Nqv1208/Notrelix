using Notrelix.Domain.Common;
using Notrelix.Domain.Billing.Payments.Events;

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
