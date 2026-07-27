using System.Collections.Concurrent;

namespace Notrelix.Platform.Messaging.Reliability;

public enum CircuitState
{
    Closed,
    Open,
    HalfOpen,
}

public sealed record CircuitStateSnapshot
{
    public CircuitState State { get; init; }
    public int FailureCount { get; init; }
    public int SuccessCount { get; init; }
    public int FailureThreshold { get; init; }
    public TimeSpan OpenDuration { get; init; }
    public DateTimeOffset? OpenedAt { get; init; }
}

public sealed class CircuitBreaker : IDisposable
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _openDuration;
    private readonly int _halfOpenMaxSuccesses;
    private readonly ConcurrentDictionary<string, CircuitStateEntry> _circuits = new();
    private bool _disposed;

    public CircuitBreaker(
        int failureThreshold = 5,
        TimeSpan? openDuration = null,
        int halfOpenMaxSuccesses = 3)
    {
        _failureThreshold = failureThreshold;
        _openDuration = openDuration ?? TimeSpan.FromSeconds(30);
        _halfOpenMaxSuccesses = halfOpenMaxSuccesses;
    }

    public CircuitStateSnapshot GetState(string circuitName)
    {
        var entry = _circuits.GetOrAdd(circuitName, _ => new CircuitStateEntry());
        return new CircuitStateSnapshot
        {
            State = entry.State,
            FailureCount = entry.FailureCount,
            SuccessCount = entry.SuccessCount,
            FailureThreshold = _failureThreshold,
            OpenDuration = _openDuration,
            OpenedAt = entry.OpenedAt,
        };
    }

    public bool IsRequestAllowed(string circuitName)
    {
        var entry = _circuits.GetOrAdd(circuitName, _ => new CircuitStateEntry());

        lock (entry)
        {
            switch (entry.State)
            {
                case CircuitState.Closed:
                    return true;

                case CircuitState.Open:
                    if (entry.OpenedAt.HasValue &&
                        DateTimeOffset.UtcNow - entry.OpenedAt.Value >= _openDuration)
                    {
                        entry.State = CircuitState.HalfOpen;
                        entry.SuccessCount = 0;
                        return true;
                    }
                    return false;

                case CircuitState.HalfOpen:
                    return entry.SuccessCount < _halfOpenMaxSuccesses;

                default:
                    return false;
            }
        }
    }

    public void RecordSuccess(string circuitName)
    {
        var entry = _circuits.GetOrAdd(circuitName, _ => new CircuitStateEntry());

        lock (entry)
        {
            entry.FailureCount = 0;

            if (entry.State == CircuitState.HalfOpen)
            {
                entry.SuccessCount++;
                if (entry.SuccessCount >= _halfOpenMaxSuccesses)
                {
                    entry.State = CircuitState.Closed;
                    entry.SuccessCount = 0;
                    entry.OpenedAt = null;
                }
            }
        }
    }

    public void RecordFailure(string circuitName)
    {
        var entry = _circuits.GetOrAdd(circuitName, _ => new CircuitStateEntry());

        lock (entry)
        {
            entry.FailureCount++;
            entry.SuccessCount = 0;

            if (entry.State == CircuitState.HalfOpen || entry.FailureCount >= _failureThreshold)
            {
                entry.State = CircuitState.Open;
                entry.OpenedAt = DateTimeOffset.UtcNow;
            }
        }
    }

    public void Reset(string circuitName)
    {
        _circuits.TryRemove(circuitName, out _);
    }

    public void Dispose()
    {
        _disposed = true;
    }

    private sealed class CircuitStateEntry
    {
        public CircuitState State = CircuitState.Closed;
        public int FailureCount;
        public int SuccessCount;
        public DateTimeOffset? OpenedAt;
    }
}
