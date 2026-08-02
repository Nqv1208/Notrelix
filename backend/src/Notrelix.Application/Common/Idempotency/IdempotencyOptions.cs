namespace Notrelix.Application.Common.Idempotency;

/// <summary>
/// Configuration options for the idempotency subsystem.
/// Validated on startup.
/// </summary>
public sealed class IdempotencyOptions
{
    public const string SectionName = "Idempotency";

    /// <summary>
    /// Default expiry for completed idempotency records. Default: 24 hours.
    /// </summary>
    public TimeSpan ResultExpiry { get; init; } = TimeSpan.FromHours(24);

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
