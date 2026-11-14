using FluentAssertions;
using Notrelix.Domain.Chat.ReadStates;

namespace Notrelix.Domain.Tests.Chat.ReadStates;

/// <summary>CHT-009: the read boundary is monotonic and never passes the space's last sequence (UC-CHT-D02, D07).</summary>
public class ReadBoundaryTests
{
    [Theory]
    [InlineData(5, 10, 5)]
    [InlineData(10, 10, 10)]
    [InlineData(50, 10, 10)]
    [InlineData(0, 0, 0)]
    [InlineData(7, 0, 0)]
    public void Clamp_KeepsTheRequestedSequenceWithinTheLastSequence(long requested, long last, long expected)
    {
        ReadBoundary.Clamp(requested, last).Should().Be(expected);
    }

    [Fact]
    public void Clamp_RejectsANegativeRequest()
    {
        var act = () => ReadBoundary.Clamp(-1, 10);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Clamp_RejectsANegativeLastSequence()
    {
        var act = () => ReadBoundary.Clamp(1, -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(7, 3, 7)]
    [InlineData(7, 7, 7)]
    [InlineData(3, 7, 7)]
    [InlineData(0, 0, 0)]
    public void Advance_NeverMovesTheBoundaryBackwards(long current, long target, long expected)
    {
        ReadBoundary.Advance(current, target).Should().Be(expected);
    }

    [Fact]
    public void AMixedSequenceOfRequests_LeavesTheBoundaryAtTheHighestAcceptedValue()
    {
        const long last = 20;
        long boundary = 0;
        long previous = boundary;

        foreach (var requested in new long[] { 4, 2, 15, 9, 40, 1, 12 })
        {
            boundary = ReadBoundary.Advance(boundary, ReadBoundary.Clamp(requested, last));

            boundary.Should().BeGreaterThanOrEqualTo(previous, "a boundary never moves backwards");
            boundary.Should().BeLessThanOrEqualTo(last, "a boundary never passes the last sequence");
            previous = boundary;
        }

        boundary.Should().Be(last);
    }
}
