using FluentAssertions;
using Notrelix.Platform.Messaging.Reliability;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Reliability;

public sealed class OrderingEnforcerTests
{
    private readonly OrderingEnforcer _sut = new();

    [Fact]
    public void ValidateSequence_ShouldAllow_FirstSequence()
    {
        var result = _sut.ValidateSequence("partition-1", sequenceNumber: 1);

        result.CanProcess.Should().BeTrue();
        result.ExpectedSequence.Should().Be(1);
        result.ReceivedSequence.Should().Be(1);
    }

    [Fact]
    public void ValidateSequence_ShouldAllow_SequentialNumbers()
    {
        _sut.ValidateSequence("partition-1", 1);
        var result = _sut.ValidateSequence("partition-1", 2);

        result.CanProcess.Should().BeTrue();
    }

    [Fact]
    public void ValidateSequence_ShouldReject_Duplicate()
    {
        _sut.ValidateSequence("partition-1", 1);
        var result = _sut.ValidateSequence("partition-1", 1);

        result.CanProcess.Should().BeFalse();
        result.Reason.Should().Contain("Duplicate");
    }

    [Fact]
    public void ValidateSequence_ShouldReject_OutOfOrder()
    {
        _sut.ValidateSequence("partition-1", 1);
        _sut.ValidateSequence("partition-1", 2);
        var result = _sut.ValidateSequence("partition-1", 1);

        result.CanProcess.Should().BeFalse();
        result.ExpectedSequence.Should().Be(3);
        result.ReceivedSequence.Should().Be(1);
        result.Reason.Should().Contain("Duplicate");
    }

    [Fact]
    public void ValidateSequence_ShouldDetectGap()
    {
        _sut.ValidateSequence("partition-1", 1);
        var result = _sut.ValidateSequence("partition-1", 5);

        result.CanProcess.Should().BeFalse();
        result.Reason.Should().Contain("Gap");
    }

    [Fact]
    public void ValidateSequence_ShouldTrackSeparatePartitions()
    {
        _sut.ValidateSequence("partition-a", 1);
        _sut.ValidateSequence("partition-b", 1);

        _sut.ValidateSequence("partition-a", 2).CanProcess.Should().BeTrue();
        _sut.ValidateSequence("partition-b", 2).CanProcess.Should().BeTrue();
    }

    [Fact]
    public void GetLastSequence_ShouldReturnZero_ForUnknownPartition()
    {
        _sut.GetLastSequence("unknown").Should().Be(0);
    }

    [Fact]
    public void Reset_ShouldClearSequence()
    {
        _sut.ValidateSequence("partition-1", 1);
        _sut.Reset("partition-1");

        _sut.GetLastSequence("partition-1").Should().Be(0);
        _sut.ValidateSequence("partition-1", 1).CanProcess.Should().BeTrue();
    }
}
