using FluentAssertions;
using Notrelix.Domain.Billing.Customers;

namespace Notrelix.Domain.Tests.Billing;

public class BillingCustomerTests
{
    private readonly Guid _accountId = Guid.NewGuid();
    private readonly Guid _createdBy = Guid.NewGuid();
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var customer = BillingCustomer.Create(_accountId, "cus_stripe_123", _createdBy, _now);

        customer.AccountId.Should().Be(_accountId);
        customer.ProviderCustomerId.Should().Be("cus_stripe_123");
        customer.Status.Should().Be("Active");
        customer.CreatedBy.Should().Be(_createdBy);
        customer.CreatedAt.Should().Be(_now);
    }

    [Fact]
    public void Create_WithEmptyAccountId_ShouldThrow()
    {
        var act = () => BillingCustomer.Create(Guid.Empty, "cus_123", _createdBy, _now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyProviderCustomerId_ShouldThrow()
    {
        var act = () => BillingCustomer.Create(_accountId, "  ", _createdBy, _now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_WithEmptyCreatedBy_ShouldThrow()
    {
        var act = () => BillingCustomer.Create(_accountId, "cus_123", Guid.Empty, _now);

        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Create_ShouldImplementIAccountScoped()
    {
        var customer = BillingCustomer.Create(_accountId, "cus_123", _createdBy, _now);

        customer.Should().BeAssignableTo<IAccountScoped>();
        ((IAccountScoped)customer).AccountId.Should().Be(_accountId);
    }
}
