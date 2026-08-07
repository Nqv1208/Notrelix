using FluentAssertions;
using Notrelix.Platform.Messaging.Reliability;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Reliability;

public sealed class PoisonDetectorTests
{
    private static PoisonMessageKey Key(string eventName = "test.event") =>
        new(eventName, Guid.NewGuid());

    [Fact]
    public void SameMessageFailures_IncrementSameCounter()
    {
        var sut = new PoisonDetector(threshold: 3);
        var key = Key();

        sut.RecordFailure(key).CurrentPoisonCount.Should().Be(1);
        sut.RecordFailure(key).CurrentPoisonCount.Should().Be(2);

        var result = sut.RecordFailure(key);
        result.IsPoison.Should().BeTrue();
        result.CurrentPoisonCount.Should().Be(3);
        result.Threshold.Should().Be(3);
    }

    [Fact]
    public void DifferentMessageIds_DoNotShareCounter()
    {
        var sut = new PoisonDetector(threshold: 2);

        var first = Key("test.event");
        var second = Key("test.event");

        sut.RecordFailure(first).IsPoison.Should().BeFalse();
        sut.RecordFailure(second).IsPoison.Should().BeFalse();
        sut.RecordFailure(first).IsPoison.Should().BeTrue();
        sut.RecordFailure(second).IsPoison.Should().BeTrue();
    }

    [Fact]
    public void DifferentEventNames_DoNotShareCounter()
    {
        var sut = new PoisonDetector(threshold: 2);
        var messageId = Guid.NewGuid();

        sut.RecordFailure(new PoisonMessageKey("event.a", messageId)).IsPoison.Should().BeFalse();
        sut.RecordFailure(new PoisonMessageKey("event.b", messageId)).IsPoison.Should().BeFalse();
        sut.RecordFailure(new PoisonMessageKey("event.a", messageId)).IsPoison.Should().BeTrue();
        sut.RecordFailure(new PoisonMessageKey("event.b", messageId)).IsPoison.Should().BeTrue();
    }

    [Fact]
    public void Success_ResetsOnlyCurrentMessage()
    {
        var sut = new PoisonDetector(threshold: 2);
        var failing = Key("test.event");
        var successful = Key("test.event");

        sut.RecordFailure(failing);
        sut.RecordFailure(successful);

        sut.Reset(successful);

        sut.GetPoisonCount(successful).Should().Be(0);
        sut.GetPoisonCount(failing).Should().Be(1);

        // The reset message must fail again from zero; the other keeps its count.
        sut.RecordFailure(successful).IsPoison.Should().BeFalse();
        sut.RecordFailure(failing).IsPoison.Should().BeTrue();
    }

    [Fact]
    public void ThresholdBelowOne_IsRejected()
    {
        var act = () => new PoisonDetector(threshold: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task ConcurrentFailures_DoNotLoseUpdates()
    {
        var sut = new PoisonDetector(threshold: 1000);
        var key = Key();

        var tasks = Enumerable.Range(0, 500)
            .Select(_ => Task.Run(() => sut.RecordFailure(key)))
            .ToArray();

        await Task.WhenAll(tasks);

        sut.GetPoisonCount(key).Should().Be(500);
    }

    [Fact]
    public void EmptyEventName_IsRejected()
    {
        var act = () => new PoisonMessageKey("", Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EmptyMessageId_IsRejected()
    {
        var act = () => new PoisonMessageKey("test.event", Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetPoisonCount_UnknownKey_ReturnsZero()
    {
        var sut = new PoisonDetector();

        sut.GetPoisonCount(Key()).Should().Be(0);
    }
}
