using FluentAssertions;
using Notrelix.Domain.Billing.Rules;
using Notrelix.Domain.Billing.Entitlements;

namespace Notrelix.Domain.Tests.Billing.Rules;

public class EntitlementRulesTests
{
    [Theory]
    [InlineData(EntitlementStatus.Active)]
    public void EnsureCanEnable_WhenActive_ShouldNotThrow(EntitlementStatus status)
    {
        Action act = () => EntitlementRules.EnsureCanEnable(status);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(EntitlementStatus.Revoked)]
    [InlineData(EntitlementStatus.Disabled)]
    public void EnsureCanEnable_WhenRevokedOrDisabled_ShouldThrow(EntitlementStatus status)
    {
        Action act = () => EntitlementRules.EnsureCanEnable(status);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*restored*");
    }

    [Theory]
    [InlineData(EntitlementStatus.Active)]
    [InlineData(EntitlementStatus.Expired)]
    [InlineData(EntitlementStatus.Disabled)]
    public void EnsureCanRevoke_WhenNotRevoked_ShouldNotThrow(EntitlementStatus status)
    {
        Action act = () => EntitlementRules.EnsureCanRevoke(status);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanRevoke_WhenRevoked_ShouldThrow()
    {
        Action act = () => EntitlementRules.EnsureCanRevoke(EntitlementStatus.Revoked);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*already revoked*");
    }
}
