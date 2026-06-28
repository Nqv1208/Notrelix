using FluentAssertions;
using Notrelix.Domain.Billing.Plans;

namespace Notrelix.Domain.Tests.Billing;

public class FeatureCodeTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var code = FeatureCode.Create("board_count");
        code.Code.Should().Be("BOARD_COUNT");
    }

    [Fact]
    public void Create_ShouldNormalizeToUpper()
    {
        var code = FeatureCode.Create(" Board_Limit ");
        code.Code.Should().Be("BOARD_LIMIT");
    }

    [Fact]
    public void Create_WithEmpty_ShouldThrow()
    {
        var act = () => FeatureCode.Create("");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Equality_SameCode_ShouldBeEqual()
    {
        var c1 = FeatureCode.Create("FEATURE_X");
        var c2 = FeatureCode.Create("feature_x");
        c1.Should().Be(c2);
    }

    [Fact]
    public void Equality_DifferentCode_ShouldNotBeEqual()
    {
        var c1 = FeatureCode.Create("FEATURE_A");
        var c2 = FeatureCode.Create("FEATURE_B");
        c1.Should().NotBe(c2);
    }
}
