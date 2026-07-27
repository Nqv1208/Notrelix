using FluentAssertions;
using Notrelix.Platform.Messaging.Reliability;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Reliability;

public sealed class RetryPolicyTests
{
    [Fact]
    public void ShouldRetry_ShouldReturnTrue_WhenUnderMaxRetries()
    {
        var policy = new RetryPolicy { MaxRetries = 3 };

        policy.ShouldRetry(0).Should().BeTrue();
        policy.ShouldRetry(2).Should().BeTrue();
    }

    [Fact]
    public void ShouldRetry_ShouldReturnFalse_WhenMaxRetriesExceeded()
    {
        var policy = new RetryPolicy { MaxRetries = 3 };

        policy.ShouldRetry(3).Should().BeFalse();
        policy.ShouldRetry(5).Should().BeFalse();
    }

    [Fact]
    public void ShouldRetry_ShouldRespectDefault()
    {
        RetryPolicy.Default.ShouldRetry(0).Should().BeTrue();
        RetryPolicy.Default.ShouldRetry(4).Should().BeTrue();
        RetryPolicy.Default.ShouldRetry(5).Should().BeFalse();
    }

    [Fact]
    public void ShouldRetry_ShouldCheckRetryableExceptions()
    {
        var policy = new RetryPolicy
        {
            MaxRetries = 3,
            RetryableExceptions = new HashSet<Type> { typeof(TimeoutException) },
        };

        policy.ShouldRetry(0, new TimeoutException()).Should().BeTrue();
        policy.ShouldRetry(0, new InvalidOperationException()).Should().BeFalse();
    }

    [Fact]
    public void GetDelay_ShouldUseExponentialBackoff()
    {
        var policy = new RetryPolicy
        {
            Strategy = BackoffStrategy.Exponential,
            BaseDelay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(60),
        };

        policy.GetDelay(0).Should().Be(TimeSpan.FromSeconds(1));
        policy.GetDelay(1).Should().Be(TimeSpan.FromSeconds(2));
        policy.GetDelay(2).Should().Be(TimeSpan.FromSeconds(4));
    }

    [Fact]
    public void GetDelay_ShouldCapAtMaxDelay()
    {
        var policy = new RetryPolicy
        {
            Strategy = BackoffStrategy.Exponential,
            BaseDelay = TimeSpan.FromSeconds(30),
            MaxDelay = TimeSpan.FromSeconds(60),
        };

        policy.GetDelay(0).Should().Be(TimeSpan.FromSeconds(30));
        policy.GetDelay(1).Should().Be(TimeSpan.FromSeconds(60));
        policy.GetDelay(5).Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void GetDelay_ShouldUseFixedBackoff()
    {
        var policy = new RetryPolicy
        {
            Strategy = BackoffStrategy.Fixed,
            BaseDelay = TimeSpan.FromSeconds(3),
        };

        policy.GetDelay(0).Should().Be(TimeSpan.FromSeconds(3));
        policy.GetDelay(5).Should().Be(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void GetDelay_ShouldUseLinearBackoff()
    {
        var policy = new RetryPolicy
        {
            Strategy = BackoffStrategy.Linear,
            BaseDelay = TimeSpan.FromSeconds(2),
        };

        policy.GetDelay(0).Should().Be(TimeSpan.FromSeconds(2));
        policy.GetDelay(1).Should().Be(TimeSpan.FromSeconds(4));
        policy.GetDelay(2).Should().Be(TimeSpan.FromSeconds(6));
    }
}
