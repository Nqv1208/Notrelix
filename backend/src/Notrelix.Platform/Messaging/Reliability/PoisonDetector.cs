using System.Collections.Concurrent;

namespace Notrelix.Platform.Messaging.Reliability;

public sealed record PoisonDetectionResult
{
    public bool IsPoison { get; init; }
    public int CurrentPoisonCount { get; init; }
    public int Threshold { get; init; }
}

public sealed class PoisonDetector
{
    private readonly ConcurrentDictionary<string, int> _poisonCounts = new();
    private readonly int _threshold;

    public PoisonDetector(int threshold = 5)
    {
        _threshold = threshold;
    }

    public PoisonDetectionResult RecordFailure(string eventName, string? consumerName = null)
    {
        var key = BuildKey(eventName, consumerName);
        var count = _poisonCounts.AddOrUpdate(key, 1, (_, c) => c + 1);

        return new PoisonDetectionResult
        {
            IsPoison = count >= _threshold,
            CurrentPoisonCount = count,
            Threshold = _threshold,
        };
    }

    public void Reset(string eventName, string? consumerName = null)
    {
        var key = BuildKey(eventName, consumerName);
        _poisonCounts.TryRemove(key, out _);
    }

    public int GetPoisonCount(string eventName, string? consumerName = null)
    {
        var key = BuildKey(eventName, consumerName);
        return _poisonCounts.TryGetValue(key, out var count) ? count : 0;
    }

    private static string BuildKey(string eventName, string? consumerName)
        => consumerName is null ? eventName : $"{eventName}:{consumerName}";
}
