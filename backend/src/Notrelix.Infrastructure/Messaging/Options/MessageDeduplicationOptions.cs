namespace Notrelix.Infrastructure.Messaging.Options;

/// <summary>
/// Lease policy for durable consumer dedup claims (<c>messaging.processed_events</c>).
/// Bound from <see cref="SectionName"/> and validated on startup.
/// </summary>
public sealed class MessageDeduplicationOptions
{
    public const string SectionName = "Messaging:Deduplication";

    public const int MinProcessingClaimLeaseSeconds = 30;
    public const int MaxProcessingClaimLeaseSeconds = 86_400;

    /// <summary>
    /// How long a command-owned "Processing" claim is honoured as an in-flight
    /// attempt. Once ClaimedAt is older than this, another delivery of the same
    /// (message, consumer) identity may take the claim over. MUST exceed the
    /// longest expected consumer execution (MassTransit retry budget included),
    /// otherwise a live attempt could be reclaimed (the command's own idempotency
    /// record still prevents a second business effect, but the work is duplicated).
    /// Default 300s, aligned with the n8n provider-effect claim stale threshold.
    /// </summary>
    public int ProcessingClaimLeaseSeconds { get; init; } = 300;
}

public sealed class MessageDeduplicationOptionsValidator : IValidateOptions<MessageDeduplicationOptions>
{
    public ValidateOptionsResult Validate(string? name, MessageDeduplicationOptions options)
    {
        if (options.ProcessingClaimLeaseSeconds is < MessageDeduplicationOptions.MinProcessingClaimLeaseSeconds
            or > MessageDeduplicationOptions.MaxProcessingClaimLeaseSeconds)
        {
            return ValidateOptionsResult.Fail(
                $"{MessageDeduplicationOptions.SectionName}:ProcessingClaimLeaseSeconds must be between " +
                $"{MessageDeduplicationOptions.MinProcessingClaimLeaseSeconds} and " +
                $"{MessageDeduplicationOptions.MaxProcessingClaimLeaseSeconds} seconds.");
        }

        return ValidateOptionsResult.Success;
    }
}
