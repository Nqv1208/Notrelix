using FluentAssertions;
using Notrelix.Domain.Integrations.Rules;

namespace Notrelix.Domain.Tests.Integrations.Rules;

public class WebhookRulesTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    public void EnsureMaxRetries_WithinRange_ShouldNotThrow(int maxRetries)
    {
        Action act = () => WebhookRules.EnsureMaxRetries(maxRetries);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    [InlineData(100)]
    public void EnsureMaxRetries_OutOfRange_ShouldThrow(int maxRetries)
    {
        Action act = () => WebhookRules.EnsureMaxRetries(maxRetries);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*between*");
    }

    [Fact]
    public void EnsureUrlIsValid_WhenValid_ShouldNotThrow()
    {
        var url = Url.Create("https://example.com/webhook");

        Action act = () => WebhookRules.EnsureUrlIsValid(url);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureUrlIsValid_WhenNull_ShouldThrow()
    {
        Action act = () => WebhookRules.EnsureUrlIsValid(null!);

        act.Should().Throw<BusinessRuleException>();
    }
}
