using System.Diagnostics;

namespace Notrelix.Platform.Messaging;

internal readonly struct ValueStopwatch
{
    private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

    private readonly long _startTimestamp;

    private ValueStopwatch(long startTimestamp)
    {
        _startTimestamp = startTimestamp;
    }

    public TimeSpan Elapsed
    {
        get
        {
            var end = Stopwatch.GetTimestamp();
            var delta = end - _startTimestamp;
            var ticks = (long)(delta * TimestampToTicks);
            return new TimeSpan(ticks);
        }
    }

    public static ValueStopwatch StartNew() => new(Stopwatch.GetTimestamp());
}
