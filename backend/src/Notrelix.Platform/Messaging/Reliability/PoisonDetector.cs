using System.Collections.Concurrent;

namespace Notrelix.Platform.Messaging.Reliability;

/// <summary>
/// Identity of a poison-tracked delivery: the event name plus the message ID.
/// Distinct messages of the same event never share a poison counter.
/// </summary>
internal readonly record struct PoisonMessageKey
{
    public PoisonMessageKey(string eventName, Guid messageId)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            throw new ArgumentException("Event name is required.", nameof(eventName));
        }

        if (messageId == Guid.Empty)
        {
            throw new ArgumentException("Message id is required.", nameof(messageId));
        }

        EventName = eventName;
        MessageId = messageId;
    }

    public string EventName { get; }

    public Guid MessageId { get; }
}

public sealed record PoisonDetectionResult
{
    public bool IsPoison { get; init; }
    public int CurrentPoisonCount { get; init; }
    public int Threshold { get; init; }
}

/// <summary>
/// Process-local failure tracker keyed by event name and message ID. Counts
/// reset on success or process restart; this is not durable dead-letter state.
/// </summary>
internal sealed class PoisonDetector
{
    private readonly ConcurrentDictionary<PoisonMessageKey, int> _poisonCounts = new();
    private readonly int _threshold;

    public PoisonDetector(int threshold = 5)
    {
        if (threshold < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), threshold,
                "Poison threshold must be at least 1.");
        }

        _threshold = threshold;
    }

    public PoisonDetectionResult RecordFailure(PoisonMessageKey key)
    {
        var count = _poisonCounts.AddOrUpdate(key, 1, (_, c) => c + 1);

        return new PoisonDetectionResult
        {
            IsPoison = count >= _threshold,
            CurrentPoisonCount = count,
            Threshold = _threshold,
        };
    }

    public void Reset(PoisonMessageKey key)
    {
        _poisonCounts.TryRemove(key, out _);
    }

    public int GetPoisonCount(PoisonMessageKey key)
    {
        return _poisonCounts.TryGetValue(key, out var count) ? count : 0;
    }
}
