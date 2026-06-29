using FluentAssertions;
using Notrelix.Domain.Billing.Entitlements;
using Notrelix.Domain.Billing.Plans;

namespace Notrelix.Domain.Tests.Billing.Entitlements;

public class EntitlementBoundaryTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Entitlement_Create_WithNegativeLimit_ShouldThrow()
    {
        var feature = FeatureCode.Create("boards");
        var act = () => Entitlement.Create(WsA, feature, -1, EntitlementSource.Subscription, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*negative*");
    }

    [Fact]
    public void Entitlement_Create_WithZeroLimit_ShouldSucceed()
    {
        var feature = FeatureCode.Create("boards");
        var entitlement = Entitlement.Create(WsA, feature, 0, EntitlementSource.Subscription, Now);
        entitlement.Limit.Should().Be(0);
    }
}
