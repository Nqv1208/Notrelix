using System.Collections.Concurrent;

namespace Notrelix.Platform.Messaging.Reliability;

public sealed record OrderingResult
{
    public bool CanProcess { get; init; }
    public long ExpectedSequence { get; init; }
    public long? ReceivedSequence { get; init; }
    public string? Reason { get; init; }
}

public sealed class OrderingEnforcer
{
    private readonly ConcurrentDictionary<string, long> _lastSequences = new();

    public OrderingResult ValidateSequence(string partitionKey, long sequenceNumber)
    {
        var lastSequence = _lastSequences.GetOrAdd(partitionKey, _ => 0);

        if (sequenceNumber <= lastSequence)
        {
            return new OrderingResult
            {
                CanProcess = false,
                ExpectedSequence = lastSequence + 1,
                ReceivedSequence = sequenceNumber,
                Reason = $"Duplicate or out-of-order sequence: received {sequenceNumber}, expected > {lastSequence}",
            };
        }

        if (sequenceNumber > lastSequence + 1)
        {
            return new OrderingResult
            {
                CanProcess = false,
                ExpectedSequence = lastSequence + 1,
                ReceivedSequence = sequenceNumber,
                Reason = $"Gap in sequence: received {sequenceNumber}, expected {lastSequence + 1}",
            };
        }

        _lastSequences.TryUpdate(partitionKey, sequenceNumber, lastSequence);

        return new OrderingResult
        {
            CanProcess = true,
            ExpectedSequence = lastSequence + 1,
            ReceivedSequence = sequenceNumber,
        };
    }

    public long GetLastSequence(string partitionKey)
    {
        return _lastSequences.TryGetValue(partitionKey, out var seq) ? seq : 0;
    }

    public void Reset(string partitionKey)
    {
        _lastSequences.TryRemove(partitionKey, out _);
    }
}
