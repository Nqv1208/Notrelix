namespace Notrelix.Infrastructure.Messaging.Options;

/// <summary>
/// Batch bounds of the outbox dispatcher for ordered streams (<c>stream_key</c> set).
/// Bound from <see cref="SectionName"/> and validated on startup.
/// </summary>
public sealed class OutboxDispatchOptions
{
    public const string SectionName = "Messaging:Outbox";

    public const int MaxStreamBatchSize = 1_000;
    public const int MaxStreamMessagesPerPollLimit = 10_000;

    /// <summary>
    /// Most consecutive messages of ONE stream a poll claims and dispatches, in stream version order.
    /// Bounds how long a hot stream holds its lease and how many messages one crash can redeliver.
    /// </summary>
    public int StreamBatchSize { get; init; } = 100;

    /// <summary>
    /// Most ordered-stream messages one poll claims across ALL streams. Streams are served oldest head first and
    /// each contributes at most <see cref="StreamBatchSize"/>, so one hot stream cannot starve the others.
    /// </summary>
    public int MaxStreamMessagesPerPoll { get; init; } = 500;

    /// <summary>Most unordered messages (no <c>stream_key</c>) one claim takes.</summary>
    public int BatchSize { get; init; } = 20;

    /// <summary>How long a claimed row stays leased to its dispatcher before another may take it.</summary>
    public int ProcessingTimeoutSeconds { get; init; } = 60;

    /// <summary>Longest wait between polls when no wake signal arrives.</summary>
    public int PollIntervalSeconds { get; init; } = 1;
}

public sealed class OutboxDispatchOptionsValidator : IValidateOptions<OutboxDispatchOptions>
{
    public ValidateOptionsResult Validate(string? name, OutboxDispatchOptions options)
    {
        if (options.StreamBatchSize is < 1 or > OutboxDispatchOptions.MaxStreamBatchSize)
        {
            return ValidateOptionsResult.Fail(
                $"{OutboxDispatchOptions.SectionName}:StreamBatchSize must be between 1 and {OutboxDispatchOptions.MaxStreamBatchSize}.");
        }

        if (options.MaxStreamMessagesPerPoll < options.StreamBatchSize
            || options.MaxStreamMessagesPerPoll > OutboxDispatchOptions.MaxStreamMessagesPerPollLimit)
        {
            return ValidateOptionsResult.Fail(
                $"{OutboxDispatchOptions.SectionName}:MaxStreamMessagesPerPoll must be between StreamBatchSize " +
                $"({options.StreamBatchSize}) and {OutboxDispatchOptions.MaxStreamMessagesPerPollLimit}.");
        }

        if (options.BatchSize is < 1 or > OutboxDispatchOptions.MaxStreamBatchSize)
        {
            return ValidateOptionsResult.Fail(
                $"{OutboxDispatchOptions.SectionName}:BatchSize must be between 1 and {OutboxDispatchOptions.MaxStreamBatchSize}.");
        }

        if (options.ProcessingTimeoutSeconds is < 5 or > 3_600)
        {
            return ValidateOptionsResult.Fail(
                $"{OutboxDispatchOptions.SectionName}:ProcessingTimeoutSeconds must be between 5 and 3600.");
        }

        if (options.PollIntervalSeconds is < 1 or > 60)
        {
            return ValidateOptionsResult.Fail(
                $"{OutboxDispatchOptions.SectionName}:PollIntervalSeconds must be between 1 and 60.");
        }

        return ValidateOptionsResult.Success;
    }
}
