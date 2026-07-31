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

    [CoversMutation(typeof(PaymentMethod), nameof(PaymentMethod.SetAsDefault), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void SetAsDefault_ShouldSetFlag_WhenNotDefault()
    {
        var method = PaymentMethod.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentProvider.Stripe, "pm_1", "4242", "Visa", Guid.NewGuid(), DateTimeOffset.UtcNow, isDefault: false);
        method.IsDefault.Should().BeFalse();

        method.SetAsDefault(Guid.NewGuid(), DateTimeOffset.UtcNow);

        method.IsDefault.Should().BeTrue();
    }

    [CoversMutation(typeof(PaymentMethod), nameof(PaymentMethod.UnsetAsDefault), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void UnsetAsDefault_ShouldClearFlag_WhenDefault()
    {
        var method = PaymentMethod.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentProvider.Stripe, "pm_1", "4242", "Visa", Guid.NewGuid(), DateTimeOffset.UtcNow, isDefault: true);
        method.IsDefault.Should().BeTrue();

        method.UnsetAsDefault(Guid.NewGuid(), DateTimeOffset.UtcNow);

        method.IsDefault.Should().BeFalse();
    }

    [CoversMutation(typeof(PaymentMethod), nameof(PaymentMethod.Deactivate), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Deactivate_ShouldExpireMethod()
    {
        var method = PaymentMethod.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentProvider.Stripe, "pm_1", "4242", "Visa", Guid.NewGuid(), DateTimeOffset.UtcNow);
        method.Status.Should().Be(PaymentMethodStatus.Active);

        method.Deactivate(Guid.NewGuid(), DateTimeOffset.UtcNow);

        method.Status.Should().Be(PaymentMethodStatus.Expired);
    }

    [CoversMutation(typeof(PaymentMethod), nameof(PaymentMethod.Reactivate), MutationScenario.Valid, typeof(Guid), typeof(DateTimeOffset))]
    [Fact]
    public void Reactivate_ShouldRestoreMethod_WhenExpired()
    {
        var method = PaymentMethod.Create(Guid.NewGuid(), Guid.NewGuid(), PaymentProvider.Stripe, "pm_1", "4242", "Visa", Guid.NewGuid(), DateTimeOffset.UtcNow);
        method.Deactivate(Guid.NewGuid(), DateTimeOffset.UtcNow);

        method.Reactivate(Guid.NewGuid(), DateTimeOffset.UtcNow);

        method.Status.Should().Be(PaymentMethodStatus.Active);
    }
}
