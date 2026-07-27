using FluentAssertions;
using Notrelix.Platform.Messaging.Reliability;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Reliability;

public sealed class PoisonDetectorTests
{
    [Fact]
    public void RecordFailure_ShouldNotDetectPoison_WhenUnderThreshold()
    {
        var sut = new PoisonDetector(threshold: 3);

        var result = sut.RecordFailure("test.event");

        result.IsPoison.Should().BeFalse();
        result.CurrentPoisonCount.Should().Be(1);
    }

    [Fact]
    public void RecordFailure_ShouldDetectPoison_WhenThresholdExceeded()
    {
        var sut = new PoisonDetector(threshold: 3);

        sut.RecordFailure("test.event");
        sut.RecordFailure("test.event");
        var result = sut.RecordFailure("test.event");

        result.IsPoison.Should().BeTrue();
        result.CurrentPoisonCount.Should().Be(3);
    }

    [Fact]
    public void RecordFailure_ShouldTrackSeparateCounts_ByConsumer()
    {
        var sut = new PoisonDetector(threshold: 2);

        sut.RecordFailure("test.event", "consumer1").IsPoison.Should().BeFalse();
        sut.RecordFailure("test.event", "consumer2").IsPoison.Should().BeFalse();
        sut.RecordFailure("test.event", "consumer1").IsPoison.Should().BeTrue();
        sut.RecordFailure("test.event", "consumer2").IsPoison.Should().BeTrue();
    }

    [Fact]
    public void Reset_ShouldClearPoisonCount()
    {
        var sut = new PoisonDetector(threshold: 2);

        sut.RecordFailure("test.event");
        sut.RecordFailure("test.event");

        sut.Reset("test.event");

        sut.GetPoisonCount("test.event").Should().Be(0);
    }

    [Fact]
    public void GetPoisonCount_ShouldReturnZero_ForUnknownEvent()
    {
        var sut = new PoisonDetector();

        sut.GetPoisonCount("unknown.event").Should().Be(0);
    }
}
