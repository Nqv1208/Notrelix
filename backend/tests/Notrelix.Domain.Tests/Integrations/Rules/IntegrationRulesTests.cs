using FluentAssertions;
using Notrelix.Domain.Integrations.Rules;
using Notrelix.Domain.Integrations.Connections;

namespace Notrelix.Domain.Tests.Integrations.Rules;

public class IntegrationRulesTests
{
    [Theory]
    [InlineData(IntegrationConnectionStatus.Expired)]
    [InlineData(IntegrationConnectionStatus.Revoked)]
    [InlineData(IntegrationConnectionStatus.Error)]
    public void EnsureCanReconnect_WhenNotActive_ShouldNotThrow(IntegrationConnectionStatus status)
    {
        Action act = () => IntegrationRules.EnsureCanReconnect(status);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCanReconnect_WhenActive_ShouldThrow()
    {
        Action act = () => IntegrationRules.EnsureCanReconnect(IntegrationConnectionStatus.Active);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*already active*");
    }

    [Fact]
    public void EnsureExpirationInFuture_WhenFuture_ShouldNotThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(1);

        Action act = () => IntegrationRules.EnsureExpirationInFuture(expiresAt, now);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureExpirationInFuture_WhenSameAsNow_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;

        Action act = () => IntegrationRules.EnsureExpirationInFuture(now, now);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*future*");
    }

    [Fact]
    public void EnsureExpirationInFuture_WhenPast_ShouldThrow()
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddDays(-1);

        Action act = () => IntegrationRules.EnsureExpirationInFuture(expiresAt, now);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*future*");
    }
}
