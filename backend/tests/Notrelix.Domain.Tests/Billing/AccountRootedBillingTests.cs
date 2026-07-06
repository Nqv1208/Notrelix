using FluentAssertions;
using Notrelix.Domain.Billing.Subscriptions;
using Notrelix.Domain.Billing.Entitlements;
using Notrelix.Domain.Billing.Plans;

namespace Notrelix.Domain.Tests.Billing;

public class AccountRootedBillingTests
{
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid PlanId = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Subscription_Create_RequiresAccountId()
    {
        var subscription = Subscription.Create(AccountId, PlanId, SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now);

        subscription.AccountId.Should().Be(AccountId);
        subscription.WorkspaceId.Should().BeNull();
    }

    [Fact]
    public void Subscription_Create_WithOptionalWorkspace_ShouldSet()
    {
        var workspaceId = Guid.NewGuid();
        var subscription = Subscription.Create(AccountId, PlanId, SubscriptionTier.Pro, Now, Now.AddDays(30), Actor, Now, workspaceId: workspaceId);

        subscription.AccountId.Should().Be(AccountId);
        subscription.WorkspaceId.Should().Be(workspaceId);
    }

    [Fact]
    public void Entitlement_Create_AccountScoped_ShouldNotRequireWorkspace()
    {
        var feature = FeatureCode.Create("boards");
        var entitlement = Entitlement.Create(AccountId, feature, 10, EntitlementSource.Subscription, Now, EntitlementTargetScope.Account);

        entitlement.AccountId.Should().Be(AccountId);
        entitlement.TargetScope.Should().Be(EntitlementTargetScope.Account);
        entitlement.TargetWorkspaceId.Should().BeNull();
    }

    [Fact]
    public void Entitlement_Create_WorkspaceScoped_RequiresTargetWorkspace()
    {
        var feature = FeatureCode.Create("boards");
        var workspaceId = Guid.NewGuid();
        var entitlement = Entitlement.Create(AccountId, feature, 10, EntitlementSource.Subscription, Now, EntitlementTargetScope.Workspace, workspaceId);

        entitlement.AccountId.Should().Be(AccountId);
        entitlement.TargetScope.Should().Be(EntitlementTargetScope.Workspace);
        entitlement.TargetWorkspaceId.Should().Be(workspaceId);
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
