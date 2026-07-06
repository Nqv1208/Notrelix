using FluentAssertions;
using Notrelix.Domain.Billing.Plans;
using Notrelix.Domain.Billing.Usage;

namespace Notrelix.Domain.Tests.Billing.Usage;

public class UsageBoundaryTests
{
    private static readonly Guid WsA = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void Usage_Consume_WithZeroAmount_ShouldThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), WsA, FeatureCode.Create("storage"), 0, 100, null, Now);
        var act = () => usage.Consume(0, Actor, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*positive*");
    }

    [Fact]
    public void Usage_Release_WithZeroAmount_ShouldThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), WsA, FeatureCode.Create("storage"), 50, 100, null, Now);
        var act = () => usage.Release(0, Actor, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*positive*");
    }

    [Fact]
    public void Usage_Consume_WithNegativeAmount_ShouldThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), WsA, FeatureCode.Create("storage"), 0, 100, null, Now);
        var act = () => usage.Consume(-5, Actor, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*positive*");
    }

    [Fact]
    public void Usage_Release_WithNegativeAmount_ShouldThrow()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), WsA, FeatureCode.Create("storage"), 50, 100, null, Now);
        var act = () => usage.Release(-5, Actor, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*positive*");
    }

    [Fact]
    public void Usage_Create_WithNegativeCurrentUsage_ShouldThrow()
    {
        var act = () => WorkspaceFeatureUsage.Create(Guid.NewGuid(), WsA, FeatureCode.Create("storage"), -1, 100, null, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*negative*");
    }

    [Fact]
    public void Usage_Create_WithNegativeHardLimit_ShouldThrow()
    {
        var act = () => WorkspaceFeatureUsage.Create(Guid.NewGuid(), WsA, FeatureCode.Create("storage"), 0, -1, null, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*negative*");
    }

    [Fact]
    public void Usage_Create_WithNegativeSoftLimit_ShouldThrow()
    {
        var act = () => WorkspaceFeatureUsage.Create(Guid.NewGuid(), WsA, FeatureCode.Create("storage"), 0, 100, -1, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*negative*");
    }

    [Fact]
    public void Usage_Create_WithSoftExceedingHard_ShouldThrow()
    {
        var act = () => WorkspaceFeatureUsage.Create(Guid.NewGuid(), WsA, FeatureCode.Create("storage"), 0, 100, 150, Now);
        act.Should().Throw<BusinessRuleException>().WithMessage("*soft limit*");
    }

    [Fact]
    public void Usage_Create_WithUsageExceedingHardLimit_WhenOverageDisallowed_ShouldThrow()
    {
        var act = () => WorkspaceFeatureUsage.Create(Guid.NewGuid(), WsA, FeatureCode.Create("storage"), 150, 100, null, Now, overageAllowed: false);
        act.Should().Throw<BusinessRuleException>().WithMessage("*overage*");
    }

    [Fact]
    public void Usage_Create_WithUsageExceedingHardLimit_WhenOverageAllowed_ShouldSucceed()
    {
        var usage = WorkspaceFeatureUsage.Create(Guid.NewGuid(), WsA, FeatureCode.Create("storage"), 150, 100, null, Now, overageAllowed: true);
        usage.CurrentUsage.Should().Be(150);
    }
}
