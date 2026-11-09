namespace Notrelix.Infrastructure.Integrations.Connections;

/// <summary>
/// Reads a receiver's Retry-After wait (UC-INT-L01). The receiver's own wait is honored, but never longer than
/// <see cref="Cap"/>: the product text names no cap, so one hour is the proposal (owner question on the cap).
/// </summary>
public static class WebhookRetryAfter
{
    public static readonly TimeSpan Cap = TimeSpan.FromHours(1);

    /// <summary>
    /// The wait a Retry-After value asks for, capped. <paramref name="delta"/> is the seconds form and
    /// <paramref name="date"/> the HTTP-date form; a date in the past means no wait. Returns null when the
    /// receiver sent neither.
    /// </summary>
    public static TimeSpan? Capped(TimeSpan? delta, DateTimeOffset? date, DateTimeOffset now)
    {
        TimeSpan? wait = delta ?? (date.HasValue ? date.Value - now : null);
        if (wait is null) return null;
        if (wait.Value < TimeSpan.Zero) return TimeSpan.Zero;
        return wait.Value > Cap ? Cap : wait.Value;
    }
}
