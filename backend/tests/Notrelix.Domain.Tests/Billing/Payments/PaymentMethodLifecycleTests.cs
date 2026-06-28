using FluentAssertions;
using Notrelix.Domain.Billing.Payments;

namespace Notrelix.Domain.Tests.Billing.Payments;

public class PaymentMethodLifecycleTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void PaymentMethod_Create_ShouldRaiseEvent()
    {
        var method = PaymentMethod.Create(WsA, PaymentProvider.Stripe, "pm_123", "4242", "Visa", Actor, Now);

        method.DomainEvents.Should().ContainSingle(e => e is PaymentMethodAddedDomainEvent);
        var evt = (PaymentMethodAddedDomainEvent)method.DomainEvents.Single(e => e is PaymentMethodAddedDomainEvent);
        evt.WorkspaceId.Should().Be(WsA);
        evt.PaymentMethodId.Should().Be(method.Id);
        evt.Provider.Should().Be(PaymentProvider.Stripe);
        evt.Last4.Should().Be("4242");
        evt.Brand.Should().Be("Visa");
    }

    [Fact]
    public void PaymentMethod_SoftDelete_ShouldIncrementVersion()
    {
        var method = PaymentMethod.Create(WsA, PaymentProvider.Stripe, "pm_123", "4242", "Visa", Actor, Now);
        method.ClearDomainEvents();
        var version = method.Version;

        method.SoftDelete(Actor, Now);

        method.IsDeleted.Should().BeTrue();
        method.Version.Should().Be(version + 1);
    }

    [Fact]
    public void PaymentMethod_Restore_ShouldIncrementVersion()
    {
        var method = PaymentMethod.Create(WsA, PaymentProvider.Stripe, "pm_123", "4242", "Visa", Actor, Now);
        method.SoftDelete(Actor, Now);
        method.ClearDomainEvents();
        var version = method.Version;

        method.Restore(Actor, Now);

        method.IsDeleted.Should().BeFalse();
        method.Version.Should().Be(version + 1);
    }

    [Fact]
    public void PaymentMethod_SoftDelete_WhenAlreadyDeleted_ShouldNotIncrement()
    {
        var method = PaymentMethod.Create(WsA, PaymentProvider.Stripe, "pm_123", "4242", "Visa", Actor, Now);
        method.SoftDelete(Actor, Now);
        method.ClearDomainEvents();
        var version = method.Version;

        method.SoftDelete(Actor, Now);

        method.Version.Should().Be(version);
    }

    [Fact]
    public void PaymentMethod_Restore_WhenNotDeleted_ShouldNotIncrement()
    {
        var method = PaymentMethod.Create(WsA, PaymentProvider.Stripe, "pm_123", "4242", "Visa", Actor, Now);
        method.ClearDomainEvents();
        var version = method.Version;

        method.Restore(Actor, Now);

        method.Version.Should().Be(version);
    }
}
