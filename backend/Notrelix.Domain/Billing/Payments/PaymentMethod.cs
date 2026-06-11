using Notrelix.Domain.Common;

namespace Notrelix.Domain.Billing.Payments;

public class PaymentMethod : AggregateRoot
{
    public Guid WorkspaceId { get; private set; }
    public PaymentProvider Provider { get; private set; }
    public string ProviderMethodId { get; private set; } = null!;
    public string Last4 { get; private set; } = null!;
    public string Brand { get; private set; } = null!;
    public PaymentMethodStatus Status { get; private set; }
    public bool IsDefault { get; private set; }

    private PaymentMethod() : base() { }

    public static PaymentMethod Create(Guid workspaceId, PaymentProvider provider, string providerMethodId, string last4, string brand, bool isDefault = false)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(providerMethodId);

        return new PaymentMethod
        {
            WorkspaceId = workspaceId,
            Provider = provider,
            ProviderMethodId = providerMethodId,
            Last4 = last4,
            Brand = brand,
            Status = PaymentMethodStatus.Active,
            IsDefault = isDefault
        };
    }
}
