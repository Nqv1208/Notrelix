using FluentAssertions;
using Notrelix.Domain.Billing.Usage;
using Notrelix.Domain.Common.Exceptions;
using Xunit;

namespace Notrelix.Domain.Tests.Billing;

public class UsageMetricKeyTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var key = UsageMetricKey.Create("api_calls");
        key.Value.Should().Be("API_CALLS");
    }

    [Fact]
    public void Create_ShouldNormalizeToUpper()
    {
        var key = UsageMetricKey.Create(" Api_Limit ");
        key.Value.Should().Be("API_LIMIT");
    }

    [Fact]
    public void Create_WithEmpty_ShouldThrow()
    {
        var act = () => UsageMetricKey.Create("");
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Equality_SameKey_ShouldBeEqual()
    {
        var k1 = UsageMetricKey.Create("BOARDS");
        var k2 = UsageMetricKey.Create("boards");
        k1.Should().Be(k2);
    }
}
