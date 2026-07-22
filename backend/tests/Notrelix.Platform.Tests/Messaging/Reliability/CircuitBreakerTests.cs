using FluentAssertions;
using Notrelix.Platform.Messaging.Reliability;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Reliability;

public sealed class CircuitBreakerTests
{
    [Fact]
    public void IsRequestAllowed_ShouldReturnTrue_WhenClosed()
    {
        var sut = new CircuitBreaker(failureThreshold: 3);

        sut.IsRequestAllowed("test").Should().BeTrue();
    }

    [Fact]
    public void RecordFailure_ShouldOpen_WhenThresholdExceeded()
    {
        var sut = new CircuitBreaker(failureThreshold: 3);

        sut.RecordFailure("test");
        sut.RecordFailure("test");
        sut.RecordFailure("test");

        sut.IsRequestAllowed("test").Should().BeFalse();
        sut.GetState("test").State.Should().Be(CircuitState.Open);
    }

    [Fact]
    public void IsRequestAllowed_ShouldReturnFalse_WhenOpen()
    {
        var sut = new CircuitBreaker(failureThreshold: 1, openDuration: TimeSpan.FromHours(1));

        sut.RecordFailure("test");

        sut.IsRequestAllowed("test").Should().BeFalse();
    }

    [Fact]
    public void RecordSuccess_ShouldClose_WhenHalfOpen()
    {
        var sut = new CircuitBreaker(
            failureThreshold: 1,
            halfOpenMaxSuccesses: 2);

        sut.RecordFailure("test");
        sut.IsRequestAllowed("test").Should().BeFalse();
    }

    [Fact]
    public void GetState_ShouldReturnCorrectSnapshot()
    {
        var sut = new CircuitBreaker(failureThreshold: 1, openDuration: TimeSpan.FromSeconds(30));

        sut.RecordFailure("test");

        var state = sut.GetState("test");
        state.State.Should().Be(CircuitState.Open);
        state.FailureCount.Should().Be(1);
        state.FailureThreshold.Should().Be(1);
        state.OpenDuration.Should().Be(TimeSpan.FromSeconds(30));
        state.OpenedAt.Should().NotBeNull();
    }

    [Fact]
    public void Reset_ShouldClearCircuitState()
    {
        var sut = new CircuitBreaker(failureThreshold: 1);

        sut.RecordFailure("test");
        sut.Reset("test");

        sut.GetState("test").State.Should().Be(CircuitState.Closed);
        sut.IsRequestAllowed("test").Should().BeTrue();
    }
}
