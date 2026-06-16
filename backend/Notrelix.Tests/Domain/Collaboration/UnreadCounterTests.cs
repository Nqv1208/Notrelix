using FluentAssertions;
using Notrelix.Domain.Collaboration.Notifications;
using Notrelix.Domain.Common.Exceptions;
using Xunit;

namespace Notrelix.Domain.Tests.Collaboration;

public class UnreadCounterTests
{
    [Fact]
    public void Create_ShouldSucceed()
    {
        var counter = UnreadCounter.Create(Guid.NewGuid(), Guid.NewGuid(), UnreadCounterType.Notification, DateTimeOffset.UtcNow);

        counter.CounterValue.Should().Be(0);
        counter.CounterType.Should().Be(UnreadCounterType.Notification);
    }

    [Fact]
    public void Increment_ShouldIncreaseValue()
    {
        var counter = CreateCounter();

        counter.Increment(DateTimeOffset.UtcNow);

        counter.CounterValue.Should().Be(1);
    }

    [Fact]
    public void Increment_MultipleTimes_ShouldStack()
    {
        var counter = CreateCounter();

        counter.Increment(DateTimeOffset.UtcNow);
        counter.Increment(DateTimeOffset.UtcNow);
        counter.Increment(DateTimeOffset.UtcNow);

        counter.CounterValue.Should().Be(3);
    }

    [Fact]
    public void Decrement_ShouldDecreaseValue()
    {
        var counter = CreateCounter();
        counter.SetValue(5, DateTimeOffset.UtcNow);

        counter.Decrement(DateTimeOffset.UtcNow);

        counter.CounterValue.Should().Be(4);
    }

    [Fact]
    public void Decrement_WhenZero_ShouldNotGoNegative()
    {
        var counter = CreateCounter();

        counter.Decrement(DateTimeOffset.UtcNow);

        counter.CounterValue.Should().Be(0);
    }

    [Fact]
    public void Reset_ShouldSetToZero()
    {
        var counter = CreateCounter();
        counter.SetValue(10, DateTimeOffset.UtcNow);

        counter.Reset(DateTimeOffset.UtcNow);

        counter.CounterValue.Should().Be(0);
    }

    [Fact]
    public void SetValue_ShouldSetExactValue()
    {
        var counter = CreateCounter();

        counter.SetValue(42, DateTimeOffset.UtcNow);

        counter.CounterValue.Should().Be(42);
    }

    [Fact]
    public void SetValue_WithNegative_ShouldThrow()
    {
        var counter = CreateCounter();

        var act = () => counter.SetValue(-1, DateTimeOffset.UtcNow);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static UnreadCounter CreateCounter()
    {
        return UnreadCounter.Create(Guid.NewGuid(), Guid.NewGuid(), UnreadCounterType.Mention, DateTimeOffset.UtcNow);
    }
}
