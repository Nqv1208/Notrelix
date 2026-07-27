using FluentAssertions;
using Notrelix.Domain.Billing.Rules;

namespace Notrelix.Domain.Tests.Billing.Rules;

public class UsageRulesTests
{
    [Fact]
    public void EnsureCanIncrease_WithinHardLimit_ShouldNotThrow()
    {
        Action act = () => UsageRules.EnsureCanIncrease(5, 3, 10, isHardLimit: true);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanIncrease_ExceedsHardLimit_ShouldThrow()
    {
        Action act = () => UsageRules.EnsureCanIncrease(8, 5, 10, isHardLimit: true);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*limit exceeded*");
    }

    [Fact]
    public void EnsureCanIncrease_AtExactLimit_ShouldNotThrow()
    {
        Action act = () => UsageRules.EnsureCanIncrease(7, 3, 10, isHardLimit: true);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanIncrease_SoftLimit_ShouldNotThrow()
    {
        Action act = () => UsageRules.EnsureCanIncrease(8, 5, 10, isHardLimit: false);

        act.Should().NotThrow();
    }
}
