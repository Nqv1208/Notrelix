namespace Notrelix.Platform.Messaging.Operations;

internal sealed class ReplayThrottle
{
    private readonly int _maxPerSecond;
    private readonly TimeSpan _minInterval;
    private DateTimeOffset _lastEventTime;

    public ReplayThrottle(int maxPerSecond)
    {
        _maxPerSecond = Math.Max(1, maxPerSecond);
        _minInterval = TimeSpan.FromSeconds(1.0 / _maxPerSecond);
        _lastEventTime = DateTimeOffset.MinValue;
    }

    public async ValueTask WaitAsync(CancellationToken cancellationToken = default)
    {
        if (_maxPerSecond <= 0)
            return;

        var now = DateTimeOffset.UtcNow;
        var elapsed = now - _lastEventTime;

        if (elapsed < _minInterval)
        {
            var delay = _minInterval - elapsed;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, cancellationToken);
        }

        _lastEventTime = DateTimeOffset.UtcNow;
    }
}
