using Microsoft.Extensions.Options;

using Notrelix.Infrastructure.Messaging.Options;

namespace Notrelix.Infrastructure.Tests.Options;

public sealed class OutboxDispatchOptionsValidatorTests
{
    private static ValidateOptionsResult Validate(OutboxDispatchOptions options) =>
        new OutboxDispatchOptionsValidator().Validate(null, options);

    [Fact]
    public void The_defaults_are_valid_and_keep_the_shipped_dispatch_behaviour()
    {
        var options = new OutboxDispatchOptions();

        Validate(options).Succeeded.Should().BeTrue();
        options.BatchSize.Should().Be(20);
        options.ProcessingTimeoutSeconds.Should().Be(60);
        options.PollIntervalSeconds.Should().Be(1);
    }

    [Theory]
    [InlineData(0, 60, 1)]
    [InlineData(1_001, 60, 1)]
    [InlineData(20, 4, 1)]
    [InlineData(20, 3_601, 1)]
    [InlineData(20, 60, 0)]
    [InlineData(20, 60, 61)]
    public void A_value_outside_its_bound_fails_startup_validation(int batch, int timeout, int poll)
    {
        var result = Validate(new OutboxDispatchOptions
        {
            BatchSize = batch,
            ProcessingTimeoutSeconds = timeout,
            PollIntervalSeconds = poll,
        });

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain(OutboxDispatchOptions.SectionName);
    }
}
