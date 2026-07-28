using FluentAssertions;
using Notrelix.Domain.Tests.Freeze;
using Notrelix.Domain.Billing.Payments;

namespace Notrelix.Domain.Tests.Billing;

[CoversAggregate(typeof(PaymentMethod))]
public class PaymentMethodTests
{
    [Fact]
    public void Create_ShouldSucceed_AndRaiseEvent()
    {
        var method = PaymentMethod.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentProvider.Stripe,
            "pm_123",
            "4242",
            "Visa",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            isDefault: true);

        method.Provider.Should().Be(PaymentProvider.Stripe);
        method.Last4.Should().Be("4242");
        method.Brand.Should().Be("Visa");
        method.IsDefault.Should().BeTrue();
        method.Status.Should().Be(PaymentMethodStatus.Active);
        method.DomainEvents.Should().ContainSingle(e => e is PaymentMethodAddedDomainEvent);
    }

    [Fact]
    public void Create_WithEmptyProviderMethodId_ShouldThrow()
    {
        var act = () => PaymentMethod.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentProvider.PayPal,
            "",
            "0000",
            "Unknown",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void SoftDelete_ShouldMarkDeleted()
    {
        var method = PaymentMethod.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentProvider.Stripe,
            "pm_456",
            "1111",
            "Amex",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        method.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        method.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_ShouldBeNoOp()
    {
        var method = PaymentMethod.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentProvider.Manual,
            "manual_1",
            "9999",
            "Manual",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
        method.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        method.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        method.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void Restore_ShouldRestore()
    {
        var method = PaymentMethod.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentProvider.Stripe,
            "pm_789",
            "5555",
            "MC",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
        method.SoftDelete(Guid.NewGuid(), DateTimeOffset.UtcNow);

        method.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        method.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Restore_WhenNotDeleted_ShouldBeNoOp()
    {
        var method = PaymentMethod.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            PaymentProvider.Stripe,
            "pm_000",
            "1234",
            "Visa",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
        ((IHasDomainEvents)method).ClearDomainEvents();

        method.Restore(Guid.NewGuid(), DateTimeOffset.UtcNow);

        method.IsDeleted.Should().BeFalse();
        method.DomainEvents.Should().BeEmpty();
    }
}
