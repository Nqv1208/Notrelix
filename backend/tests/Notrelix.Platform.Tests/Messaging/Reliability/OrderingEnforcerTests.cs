using FluentAssertions;
using Notrelix.Platform.Messaging.Reliability;
using Xunit;

namespace Notrelix.Platform.Tests.Messaging.Reliability;

public sealed class OrderingEnforcerTests
{
    private readonly OrderingEnforcer _sut = new();

    [Fact]
    public async Task Acquire_SequenceOne_ReturnsLease()
    {
        var result = await _sut.AcquireAsync("partition-1", 1, CancellationToken.None);

        result.Outcome.Should().Be(OrderingAcquisitionOutcome.Allowed);
        result.Lease.Should().NotBeNull();
        result.Lease!.SequenceNumber.Should().Be(1);
        result.ExpectedSequence.Should().Be(1);
        result.ReceivedSequence.Should().Be(1);

        await result.Lease.DisposeAsync();
    }

    [Fact]
    public async Task Commit_AdvancesLastCommittedSequence()
    {
        var result = await _sut.AcquireAsync("partition-1", 1, CancellationToken.None);

        result.Lease!.Commit();

        _sut.GetLastCommittedSequence("partition-1").Should().Be(1);

        await result.Lease.DisposeAsync();
    }

    [Fact]
    public async Task DisposeWithoutCommit_DoesNotAdvanceSequence()
    {
        var result = await _sut.AcquireAsync("partition-1", 1, CancellationToken.None);

        await result.Lease!.DisposeAsync();

        _sut.GetLastCommittedSequence("partition-1").Should().BeNull();
    }

    [Fact]
    public async Task RetrySameSequenceAfterFailure_IsAllowed()
    {
        var first = await _sut.AcquireAsync("partition-1", 1, CancellationToken.None);
        await first.Lease!.DisposeAsync();

        var retry = await _sut.AcquireAsync("partition-1", 1, CancellationToken.None);

        retry.Outcome.Should().Be(OrderingAcquisitionOutcome.Allowed);
        await retry.Lease!.DisposeAsync();
    }

    [Fact]
    public async Task DuplicateAfterCommit_IsRejected()
    {
        var first = await _sut.AcquireAsync("partition-1", 1, CancellationToken.None);
        first.Lease!.Commit();
        await first.Lease.DisposeAsync();

        var duplicate = await _sut.AcquireAsync("partition-1", 1, CancellationToken.None);

        duplicate.Outcome.Should().Be(OrderingAcquisitionOutcome.Duplicate);
        duplicate.Lease.Should().BeNull();
        duplicate.ExpectedSequence.Should().Be(2);
        duplicate.ReceivedSequence.Should().Be(1);
    }

    [Fact]
    public async Task Gap_IsRejected()
    {
        var first = await _sut.AcquireAsync("partition-1", 1, CancellationToken.None);
        await first.Lease!.DisposeAsync();

        var gap = await _sut.AcquireAsync("partition-1", 3, CancellationToken.None);

        gap.Outcome.Should().Be(OrderingAcquisitionOutcome.Gap);
        gap.Lease.Should().BeNull();
        gap.ExpectedSequence.Should().Be(1);
        gap.ReceivedSequence.Should().Be(3);
    }

    [Fact]
    public async Task ZeroSequence_IsRejected()
    {
        var result = await _sut.AcquireAsync("partition-1", 0, CancellationToken.None);

        result.Outcome.Should().Be(OrderingAcquisitionOutcome.InvalidSequence);
        result.Lease.Should().BeNull();
    }

    [Fact]
    public async Task NegativeSequence_IsRejected()
    {
        var result = await _sut.AcquireAsync("partition-1", -5, CancellationToken.None);

        result.Outcome.Should().Be(OrderingAcquisitionOutcome.InvalidSequence);
        result.Lease.Should().BeNull();
    }

    [Fact]
    public async Task MissingPartitionKey_IsRejected()
    {
        var nullKey = await _sut.AcquireAsync(null!, 1, CancellationToken.None);
        var blankKey = await _sut.AcquireAsync("   ", 1, CancellationToken.None);

        nullKey.Outcome.Should().Be(OrderingAcquisitionOutcome.MissingPartitionKey);
        nullKey.Lease.Should().BeNull();
        blankKey.Outcome.Should().Be(OrderingAcquisitionOutcome.MissingPartitionKey);
        blankKey.Lease.Should().BeNull();
    }

    [Fact]
    public async Task ConcurrentSamePartition_IsSerialized()
    {
        var first = await _sut.AcquireAsync("partition-1", 1, CancellationToken.None);
        first.Outcome.Should().Be(OrderingAcquisitionOutcome.Allowed);

        var second = _sut.AcquireAsync("partition-1", 1, CancellationToken.None);

        second.IsCompleted.Should().BeFalse(
            "the partition gate must serialize acquisition until the first lease is released");

        await first.Lease!.DisposeAsync();

        var secondResult = await second;
        secondResult.Outcome.Should().Be(OrderingAcquisitionOutcome.Allowed);
        await secondResult.Lease!.DisposeAsync();
    }

    [Fact]
    public async Task CommitIsIdempotent()
    {
        var result = await _sut.AcquireAsync("partition-1", 1, CancellationToken.None);

        result.Lease!.Commit();
        result.Lease.Commit();

        _sut.GetLastCommittedSequence("partition-1").Should().Be(1);

        await result.Lease.DisposeAsync();
    }

    [Fact]
    public async Task DisposeIsIdempotent()
    {
        var result = await _sut.AcquireAsync("partition-1", 1, CancellationToken.None);

        await result.Lease!.DisposeAsync();
        await result.Lease.DisposeAsync();

        _sut.GetLastCommittedSequence("partition-1").Should().BeNull();

        var next = await _sut.AcquireAsync("partition-1", 1, CancellationToken.None);
        next.Outcome.Should().Be(OrderingAcquisitionOutcome.Allowed);
        await next.Lease!.DisposeAsync();
    }

    [Fact]
    public async Task CommitAfterDispose_IsRejected()
    {
        var result = await _sut.AcquireAsync("partition-1", 1, CancellationToken.None);

        await result.Lease!.DisposeAsync();

        var act = () => result.Lease.Commit();
        act.Should().Throw<InvalidOperationException>();
        _sut.GetLastCommittedSequence("partition-1").Should().BeNull();
    }

    [Fact]
    public async Task CommitConcurrentWithDispose_DoesNotCommitAfterGateRelease()
    {
        for (var i = 0; i < 100; i++)
        {
            var partitionKey = $"partition-{i}";
            var acquisition = await _sut.AcquireAsync(partitionKey, 1, CancellationToken.None);
            var lease = acquisition.Lease!;
            Exception? commitError = null;

            var commit = Task.Run(() =>
            {
                try
                {
                    lease.Commit();
                }
                catch (Exception ex)
                {
                    commitError = ex;
                }
            });
            var dispose = Task.Run(() => lease.DisposeAsync().AsTask());

            await Task.WhenAll(commit, dispose);

            if (commitError is null)
            {
                _sut.GetLastCommittedSequence(partitionKey).Should().Be(1,
                    "when commit wins the race it must record the sequence before disposal");
            }
            else
            {
                commitError.Should().BeOfType<InvalidOperationException>(
                    "a commit after gate release must never write the sequence");
                _sut.GetLastCommittedSequence(partitionKey).Should().BeNull();
            }

            // The gate must be released exactly once: a double release would
            // surface as SemaphoreFullException on the next acquisition.
            var expected = commitError is null ? OrderingAcquisitionOutcome.Allowed : OrderingAcquisitionOutcome.Gap;
            var next = await _sut.AcquireAsync(partitionKey, 2, CancellationToken.None);
            next.Outcome.Should().Be(expected);
            if (next.Lease is not null)
            {
                await next.Lease.DisposeAsync();
            }
        }
    }

    [Fact]
    public async Task DifferentPartitions_DoNotBlockEachOther()
    {
        var first = await _sut.AcquireAsync("partition-a", 1, CancellationToken.None);

        var other = await _sut.AcquireAsync("partition-b", 1, CancellationToken.None);

        other.Outcome.Should().Be(OrderingAcquisitionOutcome.Allowed);
        other.Lease.Should().NotBeNull();

        await first.Lease!.DisposeAsync();
        await other.Lease!.DisposeAsync();
    }

    [Fact]
    public void GetLastCommittedSequence_UnknownPartition_ReturnsNull()
    {
        _sut.GetLastCommittedSequence("unknown").Should().BeNull();
    }
}
