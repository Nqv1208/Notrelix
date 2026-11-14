using Microsoft.Extensions.Options;
using Notrelix.Infrastructure.Integrations.Connections;

namespace Notrelix.Infrastructure.Tests.Options;

public sealed class IntegrationsOptionsValidatorTests
{
    private static ValidateOptionsResult Validate(int timeoutSeconds) =>
        new IntegrationsOptionsValidator().Validate(
            null,
            new IntegrationsOptions { Webhook = new IntegrationWebhookOptions { TimeoutSeconds = timeoutSeconds } });

    [Fact]
    public void The_default_webhook_timeout_is_valid()
    {
        Validate(new IntegrationWebhookOptions().TimeoutSeconds).Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(120)]
    public void A_timeout_at_either_bound_is_valid(int timeoutSeconds)
    {
        Validate(timeoutSeconds).Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(121)]
    public void A_timeout_outside_the_bounds_fails_startup_validation(int timeoutSeconds)
    {
        var result = Validate(timeoutSeconds);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("Integrations:Webhook:TimeoutSeconds");
    }
}
