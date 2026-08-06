using System.Collections.Concurrent;

namespace Notrelix.Platform.Messaging.Reliability;

public enum OrderingAcquisitionOutcome
{
    Allowed,
    MissingSequence,
    InvalidSequence,
    MissingPartitionKey,
    Duplicate,
    Gap,
}

public sealed record OrderingAcquisitionResult
{
    public required OrderingAcquisitionOutcome Outcome { get; init; }
    public required long ExpectedSequence { get; init; }
    public required long ReceivedSequence { get; init; }
    public OrderingLease? Lease { get; init; }
}

internal sealed class OrderingPartitionState
{
    internal SemaphoreSlim Gate { get; } = new(1, 1);
    internal long? LastCommittedSequence { get; set; }
}

/// <summary>
/// Partition-scoped lease granted while the partition gate is held. The lease
/// must be disposed exactly once; disposal releases the partition gate. Commit
/// records the sequence and is only valid before disposal.
/// </summary>
public sealed class OrderingLease : IAsyncDisposable
{
    private readonly OrderingPartitionState _state;
    private readonly object _sync = new();
    private bool _disposed;
    private bool _committed;

    internal OrderingLease(OrderingPartitionState state, long sequenceNumber)
    {
        _state = state;
        SequenceNumber = sequenceNumber;
    }

    public long SequenceNumber { get; }

    public void Commit()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                throw new InvalidOperationException(
                    $"Cannot commit sequence {SequenceNumber}: the ordering lease has already been disposed.");
            }

            if (_committed)
            {
                return;
            }

            _state.LastCommittedSequence = SequenceNumber;
            _committed = true;
        }
    }

    public ValueTask DisposeAsync()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return ValueTask.CompletedTask;
            }

            _disposed = true;
        }

        _state.Gate.Release();
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Process-local partition serializer and sequence validator. Ordering state
/// lives for the process lifetime and does not survive restart; the transport
/// must provide partition affinity so one partition reaches one host instance.
/// Partition keys have stable, finite cardinality.
/// </summary>
public sealed class OrderingEnforcer
{
    private readonly ConcurrentDictionary<string, OrderingPartitionState> _partitions = new();

    public Task<OrderingAcquisitionResult> AcquireAsync(
        string partitionKey,
        long sequenceNumber,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(partitionKey))
        {
            return Task.FromResult(new OrderingAcquisitionResult
            {
                Outcome = OrderingAcquisitionOutcome.MissingPartitionKey,
                ExpectedSequence = 0,
                ReceivedSequence = sequenceNumber,
            });
        }

        if (sequenceNumber < 1)
        {
            return Task.FromResult(new OrderingAcquisitionResult
            {
                Outcome = OrderingAcquisitionOutcome.InvalidSequence,
                ExpectedSequence = 0,
                ReceivedSequence = sequenceNumber,
            });
        }

        return AcquireCoreAsync(partitionKey, sequenceNumber, cancellationToken);
    }

    private async Task<OrderingAcquisitionResult> AcquireCoreAsync(
        string partitionKey,
        long sequenceNumber,
        CancellationToken cancellationToken)
    {
        var state = _partitions.GetOrAdd(partitionKey, _ => new OrderingPartitionState());

        await state.Gate.WaitAsync(cancellationToken);

        var expected = (state.LastCommittedSequence ?? 0) + 1;

        if (sequenceNumber < expected)
        {
            state.Gate.Release();
            return new OrderingAcquisitionResult
            {
                Outcome = OrderingAcquisitionOutcome.Duplicate,
                ExpectedSequence = expected,
                ReceivedSequence = sequenceNumber,
            };
        }

        if (sequenceNumber > expected)
        {
            state.Gate.Release();
            return new OrderingAcquisitionResult
            {
                Outcome = OrderingAcquisitionOutcome.Gap,
                ExpectedSequence = expected,
                ReceivedSequence = sequenceNumber,
            };
        }

        return new OrderingAcquisitionResult
        {
            Outcome = OrderingAcquisitionOutcome.Allowed,
            ExpectedSequence = sequenceNumber,
            ReceivedSequence = sequenceNumber,
            Lease = new OrderingLease(state, sequenceNumber),
        };
    }

    public long? GetLastCommittedSequence(string partitionKey)
    {
        return _partitions.TryGetValue(partitionKey, out var state)
            ? state.LastCommittedSequence
            : null;
    }
}
