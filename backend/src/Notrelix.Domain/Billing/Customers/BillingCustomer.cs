namespace Notrelix.Domain.Billing.Customers;

public class BillingCustomer : AggregateRoot
{
    public Guid AccountId { get; private set; }
    public string ProviderCustomerId { get; private set; } = null!;
    public string Status { get; private set; } = "Active";

    private BillingCustomer() { }

    public static BillingCustomer Create(Guid accountId, string providerCustomerId)
    {
        Guard.NotEmpty(accountId);
        Guard.NotNullOrWhiteSpace(providerCustomerId);

        return new BillingCustomer
        {
            AccountId = accountId,
            ProviderCustomerId = providerCustomerId
        };
    }
}
