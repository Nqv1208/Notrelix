namespace Notrelix.Infrastructure.Storage;

/// <summary>
/// Retention and reconciliation policy of the attachment object reaper. Bound from
/// <c>Collaboration:Attachments:Retention</c> and validated on start. See
/// <c>backend/docs/architecture/infrastructure-and-data.md</c> (file upload).
/// </summary>
public sealed class AttachmentRetentionOptions
{
    public const string SectionName = "Collaboration:Attachments:Retention";

    /// <summary>When false the reaper never deletes anything. Default true.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// How long the object of a soft-deleted stored-file attachment is kept (counted from the
    /// deletion time) before it is purged from storage. Default 30 days.
    /// </summary>
    public TimeSpan DeletedFileRetention { get; init; } = TimeSpan.FromDays(30);

    /// <summary>
    /// Minimum age (by the provider's last-modified time, never by request state) of an object
    /// with no referencing row before it is removed as an orphan. It must be longer than the
    /// longest possible upload so an in-flight upload is never reaped. Default 24 hours.
    /// </summary>
    public TimeSpan OrphanGracePeriod { get; init; } = TimeSpan.FromHours(24);

    /// <summary>Time between reaper passes. Default 1 hour.</summary>
    public TimeSpan Interval { get; init; } = TimeSpan.FromHours(1);

    /// <summary>Maximum rows or objects handled per claim/transaction. Default 100.</summary>
    public int BatchSize { get; init; } = 100;
}

public sealed class AttachmentRetentionOptionsValidator : IValidateOptions<AttachmentRetentionOptions>
{
    public static readonly TimeSpan MinDeletedFileRetention = TimeSpan.FromHours(1);
    public static readonly TimeSpan MinOrphanGracePeriod = TimeSpan.FromHours(1);
    public static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan MaxInterval = TimeSpan.FromDays(1);
    public const int MaxBatchSize = 1000;

    public ValidateOptionsResult Validate(string? name, AttachmentRetentionOptions options)
    {
        var failures = new List<string>();
        var section = AttachmentRetentionOptions.SectionName;

        if (options.DeletedFileRetention < MinDeletedFileRetention)
        {
            failures.Add($"{section}:DeletedFileRetention must be at least {MinDeletedFileRetention}.");
        }

        if (options.OrphanGracePeriod < MinOrphanGracePeriod)
        {
            failures.Add($"{section}:OrphanGracePeriod must be at least {MinOrphanGracePeriod} (longer than the longest upload).");
        }

        if (options.Interval < MinInterval || options.Interval > MaxInterval)
        {
            failures.Add($"{section}:Interval must be between {MinInterval} and {MaxInterval}.");
        }

        if (options.BatchSize is < 1 or > MaxBatchSize)
        {
            failures.Add($"{section}:BatchSize must be between 1 and {MaxBatchSize}.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
