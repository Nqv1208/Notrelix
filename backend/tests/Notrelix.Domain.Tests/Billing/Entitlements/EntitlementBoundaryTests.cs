using FluentAssertions;
using Notrelix.Domain.Billing.Entitlements;
using Notrelix.Domain.Billing.Plans;

namespace Notrelix.Domain.Tests.Billing.Entitlements;

public class EntitlementBoundaryTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Entitlement_Create_WithNegativeLimit_ShouldThrow()
    {
        var feature = FeatureCode.Create("boards");
        var act = () => Entitlement.Create(AccountId, feature, -1, EntitlementSource.Subscription, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*negative*");
    }

    [Fact]
    public void Entitlement_Create_WithZeroLimit_ShouldSucceed()
    {
        var feature = FeatureCode.Create("boards");
        var entitlement = Entitlement.Create(AccountId, feature, 0, EntitlementSource.Subscription, Now);
        entitlement.Limit.Should().Be(0);
    }

    [Fact]
    public void Entitlement_Create_AccountScoped_ShouldNotRequireWorkspace()
    {
        var feature = FeatureCode.Create("boards");
        var entitlement = Entitlement.Create(AccountId, feature, 10, EntitlementSource.Subscription, Now, EntitlementTargetScope.Account);
        entitlement.TargetScope.Should().Be(EntitlementTargetScope.Account);
        entitlement.TargetWorkspaceId.Should().BeNull();
    }

    [Fact]
    public void Entitlement_Create_WorkspaceScoped_WithoutTarget_ShouldThrow()
    {
        var feature = FeatureCode.Create("boards");
        var act = () => Entitlement.Create(AccountId, feature, 10, EntitlementSource.Subscription, Now, EntitlementTargetScope.Workspace);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Entitlement_Create_AccountScoped_WithTarget_ShouldThrow()
    {
        var feature = FeatureCode.Create("boards");
        var act = () => Entitlement.Create(AccountId, feature, 10, EntitlementSource.Subscription, Now, EntitlementTargetScope.Account, Guid.NewGuid());
        act.Should().Throw<BusinessRuleException>();
    }
}
