namespace Notrelix.Domain.Billing.Customers;

public class BillingCustomer : AggregateRoot, IAccountScoped
{
    public Guid AccountId { get; private set; }
    public string ProviderCustomerId { get; private set; } = null!;
    public string Status { get; private set; } = "Active";

    private BillingCustomer() { }

    public static BillingCustomer Create(Guid accountId, string providerCustomerId, Guid createdBy, DateTimeOffset createdAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotNullOrWhiteSpace(providerCustomerId);
        Guard.NotEmpty(createdBy);

        var customer = new BillingCustomer
        {
            AccountId = accountId,
            ProviderCustomerId = providerCustomerId
        };

        customer.SetAuditOnCreate(createdBy, createdAt);
        return customer;
    }
}
