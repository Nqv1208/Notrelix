namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Configuration options for the idempotency subsystem.
/// Validated on startup.
/// </summary>
public sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    /// <summary>
    /// Default expiry for in-progress (Processing) idempotency records. Default: 5 minutes.
    /// </summary>
    public TimeSpan ProcessingExpiry { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Default expiry for completed idempotency records. Default: 1 day.
    /// </summary>
    public TimeSpan ResultExpiry { get; init; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Retry-After delay the API returns when a committed Processing row is detected.
    /// Default: 3 seconds.
    /// </summary>
    public TimeSpan IncompleteStateRetryAfter { get; init; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Maximum serialized result size in bytes. Results exceeding this are not cached for replay.
    /// Default: 1 MB.
    /// </summary>
    public int MaxResultBytes { get; init; } = 1_048_576;

    /// <summary>
    /// Explicit type full names that must never be replay-cached (e.g. token/auth responses).
    /// </summary>
    public IReadOnlyList<string> SensitiveResultTypes { get; init; } = [];
}
